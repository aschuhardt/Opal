using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Opal.Authentication.Certificate;
using Opal.Authentication.Database;
using Opal.Event;
using Opal.Header;
using Opal.Response;
using Opal.Tofu;

namespace Opal;

public class OpalClient : IOpalClient
{
    private const int HeaderBufferLength = 2048;
    private const int MaxRedirectDepth = 5;
    private const int DefaultPort = 1965;
    private const string Scheme = "gemini";
    private const string SchemePrefix = $"{Scheme}://";
    private const int DecryptPasswordAttempts = 5;

    private readonly IAuthenticationDatabase _authenticationDatabase;
    private readonly ICertificateDatabase _certificateDatabase;
    private readonly RedirectBehavior _redirectBehavior;

    public OpalClient(ICertificateDatabase certificateDatabase, IAuthenticationDatabase authenticationDatabase,
        RedirectBehavior redirectBehavior)
    {
        UriParser.Register(
            new GenericUriParser(GenericUriParserOptions.NoFragment | GenericUriParserOptions.Default),
            Scheme, DefaultPort);

        _certificateDatabase = certificateDatabase;
        _authenticationDatabase = authenticationDatabase;
        _redirectBehavior = redirectBehavior;

        _authenticationDatabase.CertificatePasswordRequired +=
            (sender, args) => CertificatePasswordRequired?.Invoke(sender, args);
    }

    public IGeminiResponse SendRequest(string uri)
    {
        if (!uri.StartsWith(SchemePrefix, StringComparison.InvariantCultureIgnoreCase))
            uri = SchemePrefix + uri;
        return SendUriRequest(new Uri(uri));
    }

    public IGeminiResponse SendRequest(string uri, string input)
    {
        if (!uri.StartsWith(SchemePrefix, StringComparison.InvariantCultureIgnoreCase))
            uri = SchemePrefix + uri;
        return SendUriRequest(new UriBuilder(uri) { Query = input }.Uri);
    }

    public IEnumerable<IClientCertificate> Certificates => _authenticationDatabase.Certificates;

    public void RemoveCertificate(IClientCertificate certificate)
    {
        _authenticationDatabase.Remove(certificate);
    }

    public event EventHandler<RemoteCertificateInvalidEventArgs> RemoteCertificateInvalid;
    public event EventHandler<RemoteCertificateUnrecognizedEventArgs> RemoteCertificateUnrecognized;
    public event EventHandler<CertificatePasswordRequiredEventArgs> CertificatePasswordRequired;
    public event EventHandler<SendingClientCertificateEventArgs> SendingClientCertificate;
    public event EventHandler<CertificateExpiredEventArgs> CertificateExpired;
    public event EventHandler<InputRequiredEventArgs> InputRequired;
    public event EventHandler<CertificateRequiredEventArgs> CertificateRequired;
    public event EventHandler<ConfirmRedirectEventArgs> ConfirmRedirect;

    private static byte[] ConvertToUtf8(string source)
    {
        return Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(source));
    }

    private static bool IsGemtextMimetype(string mimetype)
    {
        return mimetype.Contains(GemtextResponse.GemtextMimeType, StringComparison.InvariantCultureIgnoreCase);
    }

    private static IGeminiResponse BuildResponse(Uri uri, GeminiHeader header, Stream contents)
    {
        // make sure the status code is recognizable
        if (!Enum.IsDefined(typeof(StatusCode), header.StatusCode))
            return new InvalidResponse(uri);

        var status = (StatusCode)header.StatusCode;
        return status switch
        {
            StatusCode.Input => new InputRequiredResponse(uri, false, header.Meta),
            StatusCode.InputSensitive => new InputRequiredResponse(uri, true, header.Meta),
            StatusCode.Success => IsGemtextMimetype(header.Meta)
                ? new GemtextResponse(uri, contents, header.Languages)
                : new SuccessfulResponse(uri, contents, header.Meta),
            StatusCode.TemporaryRedirect => new RedirectResponse(uri, false, header.Meta),
            StatusCode.PermanentRedirect => new RedirectResponse(uri, true, header.Meta),
            StatusCode.ClientCertificateRequired => new ErrorResponse(uri, status, header.Meta),
            StatusCode.TemporaryFailure => new ErrorResponse(uri, status, header.Meta),
            StatusCode.ServerUnavailable => new ErrorResponse(uri, status, header.Meta),
            StatusCode.CgiError => new ErrorResponse(uri, status, header.Meta),
            StatusCode.ProxyError => new ErrorResponse(uri, status, header.Meta),
            StatusCode.SlowDown => new ErrorResponse(uri, status, header.Meta),
            StatusCode.PermanentFailure => new ErrorResponse(uri, status, header.Meta),
            StatusCode.NotFound => new ErrorResponse(uri, status, header.Meta),
            StatusCode.Gone => new ErrorResponse(uri, status, header.Meta),
            StatusCode.ProxyRequestRefused => new ErrorResponse(uri, status, header.Meta),
            StatusCode.BadRequest => new ErrorResponse(uri, status, header.Meta),
            StatusCode.CertificateNotAuthorized => new ErrorResponse(uri, status, header.Meta),
            StatusCode.CertificateNotValid => new ErrorResponse(uri, status, header.Meta),
            StatusCode.Unknown => new InvalidResponse(uri),
            _ => new InvalidResponse(uri)
        };
    }

    private bool TryGetCertificateFromDatabase(string host, out IClientCertificate cert)
    {
        cert = null;

        for (var i = 0; i < DecryptPasswordAttempts; i++)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (_authenticationDatabase.TryGetCertificate(host, out cert))
            {
                case CertificateResult.Success:
                    return true;
                case CertificateResult.DecryptionFailure:
                    continue;
                default:
                    return false;
            }
        }

        return false;
    }

    private bool IsCertificateValid(IClientCertificate cert, out IClientCertificate validated)
    {
        if (DateTime.Now > cert.Certificate.NotAfter)
        {
            // certificate has expired; present the caller with the opportunity to renew it
            var args = new CertificateExpiredEventArgs(cert);
            CertificateExpired?.Invoke(this, args);

            if (args.Replacement != null)
            {
                validated = args.Replacement;
                _authenticationDatabase.Remove(cert);
                _authenticationDatabase.Add(validated, args.Password);
                return true;
            }

            validated = null;
            return false;
        }

        validated = cert;
        return true;
    }

    private bool CanSendCertificate(IClientCertificate cert)
    {
        var args = new SendingClientCertificateEventArgs(cert);
        SendingClientCertificate?.Invoke(this, args);
        return !args.Cancel;
    }

    private IGeminiResponse SendUriRequest(Uri uri, bool allowRepeat = true, int depth = 1)
    {
        try
        {
            IGeminiResponse response;

            using (var stream = BuildSslStream(uri))
            {
                // authenticate
                stream.AuthenticateAsClient(uri.Host,
                    TryGetCertificateFromDatabase(uri.Host, out var cert) &&
                    IsCertificateValid(cert, out var validated) &&
                    CanSendCertificate(validated)
                        ? new X509Certificate2Collection(cert.Certificate)
                        : null, false);

                // send the initial request
                SendRequest(uri, stream);

                // read the response from the server
                if (TrySendSuccessResponse(uri, stream, out response))
                    return response;
            }

            return ProcessNonSuccessResponse(response, uri, allowRepeat, depth);
        }
        catch (SocketException e)
        {
            return new NetworkErrorResponse(uri, e);
        }
        catch (Exception e)
        {
            return new GeneralErrorResponse(uri, e);
        }
    }

    private static bool TrySendSuccessResponse(Uri uri, Stream stream, out IGeminiResponse response)
    {
        var bodyStream = new MemoryStream();
        var buffer = new byte[HeaderBufferLength];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);

        // the first read will contain the header; if the status is 'success', then we will copy the rest of the buffer onto the response
        var header = GeminiHeader.Parse(buffer, out var bodyBeginsAt);
        if (header == null)
        {
            response = new InvalidResponse(uri);
            return false;
        }

        response = BuildResponse(uri, header, bodyStream);

        if (response.IsSuccess)
        {
            // write the rest of the initial read (everything after the header) to the body stream
            bodyStream.Write(buffer, bodyBeginsAt, bytesRead - bodyBeginsAt);
            stream.CopyTo(bodyStream);
            bodyStream.Seek(0, SeekOrigin.Begin);
            return true;
        }

        return false;
    }

    private static void SendRequest(Uri uri, SslStream stream)
    {
        stream.Write(ConvertToUtf8(uri.ToString()));
        stream.Write(ConvertToUtf8("\r\n"));
        stream.Flush();
    }

    private IGeminiResponse ProcessNonSuccessResponse(IGeminiResponse response, Uri uri, bool allowRepeat, int depth)
    {
        if (response.IsInputRequired && allowRepeat && response is InputRequiredResponse inputResponse)
        {
            // prompt the caller to provide input, and re-send the request if any ways provided
            var args = new InputRequiredEventArgs(inputResponse.Sensitive,
                inputResponse.Message ?? "Input required");
            InputRequired?.Invoke(this, args);
            if (!string.IsNullOrEmpty(args.Value))
            {
                // caller provided input; re-send with the input
                var updatedUri = new UriBuilder(uri) { Query = args.Value }.Uri;
                return SendUriRequest(updatedUri, false);
            }
        }
        else if (response.IsCertificateRequired && allowRepeat && response is ErrorResponse errorResponse)
        {
            // prompt the caller to provide a certificate
            var args = new CertificateRequiredEventArgs(errorResponse.Message, uri.Host);
            CertificateRequired?.Invoke(this, args);

            if (args.Certificate == null)
                return response;

            // caller provided a cert; register it and re-send
            _authenticationDatabase.Add(new ClientCertificate(args.Certificate, uri.Host,
                args.Certificate.GetNameInfo(X509NameType.SimpleName, false)), args.Password);
            return SendUriRequest(uri, false);
        }
        else if (response.IsRedirect && response is RedirectResponse redirectResponse)
        {
            if (_redirectBehavior == RedirectBehavior.Ignore)
                return response;

            if (depth >= MaxRedirectDepth)
                return new ErrorResponse(uri, StatusCode.Unknown, "Too many redirects");

            // prompt the caller to confirm redirection
            var args = new ConfirmRedirectEventArgs(redirectResponse.RedirectTo, redirectResponse.IsPermanent);

            var shouldFollow = true;
            if (_redirectBehavior == RedirectBehavior.Confirm)
            {
                ConfirmRedirect?.Invoke(this, args);
                shouldFollow = args.FollowRedirect;
            }

            if (shouldFollow)
            {
                var nextUri = new Uri(redirectResponse.RedirectTo, UriKind.RelativeOrAbsolute);

                // redirects have to support relative URIs;
                if (!nextUri.IsAbsoluteUri)
                {
                    nextUri = new UriBuilder(nextUri)
                        { Scheme = uri.Scheme, Host = uri.Host, Port = uri.IsDefaultPort ? -1 : uri.Port }.Uri;
                }

                return SendUriRequest(nextUri, depth: depth + 1);
            }
        }

        return response;
    }

    private SslStream BuildSslStream(Uri uri)
    {
        var tcpClient = new TcpClient(uri.Host, uri.IsDefaultPort ? DefaultPort : uri.Port);
        return new SslStream(tcpClient.GetStream(), false,
            (_, cert, _, _) => ValidateCertificate(uri, cert),
            null, EncryptionPolicy.RequireEncryption);
    }

    private bool ValidateCertificate(Uri uri, X509Certificate certificate)
    {
        if (_certificateDatabase.IsCertificateValid(uri.Host, certificate, out var result))
            return true;

        if (result == InvalidCertificateReason.TrustedMismatch)
        {
            var args = new RemoteCertificateUnrecognizedEventArgs(uri.Host,
                certificate.GetCertHashString(HashAlgorithmName.SHA256));
            RemoteCertificateUnrecognized?.Invoke(this, args);

            if (!args.AcceptAndTrust)
                return false;

            // user opted to accept this certificate in place of the previously-recognized one,
            // so update the cache and re-validate
            _certificateDatabase.RemoveTrusted(uri.Host);
            return ValidateCertificate(uri, certificate);
        }

        RemoteCertificateInvalid?.Invoke(this, new RemoteCertificateInvalidEventArgs(uri.Host, result));
        return false;
    }

    /// <summary>
    ///     Returns a new instance of <see cref="OpalClient" /> with the provided configuration options.  For a secure default
    ///     configuration, pass <see cref="OpalOptions.Default" />.
    /// </summary>
    public static IOpalClient CreateNew(OpalOptions options)
    {
        // use default dependencies
        return new OpalClient(
            CreateCertificateDatabase(options),
            CreateAuthenticationDatabase(options),
            options.RedirectBehavior);
    }

    private static IAuthenticationDatabase CreateAuthenticationDatabase(OpalOptions options)
    {
        return options.UsePersistentAuthenticationDatabase
            ? new PersistentAuthenticationDatabase()
            : new InMemoryAuthenticationDatabase();
    }

    private static ICertificateDatabase CreateCertificateDatabase(OpalOptions options)
    {
        ICertificateDatabase certDatabase;
        if (!options.VerifyCertificates)
            certDatabase = new DummyCertificateDatabase();
        else if (options.UsePersistentCertificateDatabase)
            certDatabase = new PersistentCertificateDatabase();
        else
            certDatabase = new InMemoryCertificateDatabase();

        return certDatabase;
    }
}