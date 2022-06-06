using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Opal.Authentication.Certificate;
using Opal.Authentication.Database;
using Opal.CallbackArgs;
using Opal.Header;
using Opal.Response;
using Opal.Tofu;

namespace Opal;

public class OpalClient : IOpalClient
{
    private const int MaxRedirectDepth = 5;
    private const int DefaultPort = 1965;
    private const int Timeout = 4000;
    private const string Scheme = "gemini";
    private const string SchemePrefix = $"{Scheme}://";
    private readonly RedirectBehavior _redirectBehavior;

    public OpalClient() : this(new DummyCertificateDatabase(), new InMemoryAuthenticationDatabase(),
        RedirectBehavior.Follow)
    {
    }

    public OpalClient(ICertificateDatabase certificateDatabase, IAuthenticationDatabase authenticationDatabase,
        RedirectBehavior redirectBehavior)
    {
        if (!UriParser.IsKnownScheme("gemini"))
        {
            UriParser.Register(
                new GenericUriParser(GenericUriParserOptions.NoFragment | GenericUriParserOptions.Default),
                Scheme, DefaultPort);
        }

        CertificateDatabase = certificateDatabase;
        AuthenticationDatabase = authenticationDatabase;
        _redirectBehavior = redirectBehavior;
    }

    public IAuthenticationDatabase AuthenticationDatabase { get; }
    public ICertificateDatabase CertificateDatabase { get; }

    public Func<Task<IClientCertificate>> GetActiveCertificateCallback { get; set; }
    public Func<CertificateExpiredArgs, Task> CertificateExpiredCallback { get; set; }
    public Func<ConfirmRedirectArgs, Task> ConfirmRedirectCallback { get; set; }
    public Func<InputRequiredArgs, Task> InputRequiredCallback { get; set; }
    public Func<SendingClientCertificateArgs, Task> SendingClientCertificateCallback { get; set; }
    public Func<RemoteCertificateInvalidArgs, Task> RemoteCertificateInvalidCallback { get; set; }
    public Func<RemoteCertificateUnrecognizedArgs, Task> RemoteCertificateUnrecognizedCallback { get; set; }

    public Task<IGeminiResponse> SendRequestAsync(string uri)
    {
        if (!uri.StartsWith(SchemePrefix, StringComparison.InvariantCultureIgnoreCase))
            uri = SchemePrefix + uri;
        return SendUriRequestAsync(new Uri(uri));
    }

    public Task<IGeminiResponse> SendRequestAsync(string uri, string input)
    {
        if (!uri.StartsWith(SchemePrefix, StringComparison.InvariantCultureIgnoreCase))
            uri = SchemePrefix + uri;
        return SendUriRequestAsync(new UriBuilder(uri) { Query = input }.Uri);
    }

    private async Task<IGeminiResponse> SendUriRequestAsync(Uri uri, bool allowRepeat = true, int depth = 1)
    {
        try
        {
            IGeminiResponse response;

            await using (var stream = BuildSslStream(uri, CertificateValidationCallback))
            {
                stream.ReadTimeout = Timeout;
                stream.WriteTimeout = Timeout;

                IClientCertificate cert = null;

                if (GetActiveCertificateCallback != null)
                    cert = await GetActiveCertificateCallback();

                // authenticate
                await stream.AuthenticateAsClientAsync(uri.Host,
                    cert != null &&
                    await IsCertificateValidAsync(cert) &&
                    await CanSendCertificateAsync(cert)
                        ? new X509Certificate2Collection(cert.Certificate)
                        : null, false);

                // send the initial request
                await SendRequestAsync(uri, stream);

                // read the response from the server
                response = await ReadResponseAsync(uri, stream);

                // if successful, return immediately
                if (response is SuccessfulResponse)
                    return response;
            }

            return await ProcessNonSuccessResponseAsync(response, uri, allowRepeat, depth);
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

    private bool CertificateValidationCallback(Uri uri, X509Certificate cert)
    {
        // "thread pool hack" from https://docs.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development
        return Task.Run(() => ValidateCertificate(uri, cert)).GetAwaiter().GetResult();
    }

    private static async Task<IGeminiResponse> ReadResponseAsync(Uri uri, SslStream stream)
    {
        // read the entire response into a buffer
        var body = new MemoryStream();
        await stream.CopyToAsync(body);

        // the first line will contain the header; if the status is 'success', then we will copy the rest of the buffer onto the response
        body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(body, leaveOpen: true);
        var header = GeminiHeader.Parse(await reader.ReadLineAsync());
        return header == null
            ? new InvalidResponse(uri)
            : BuildResponse(uri, header, body);
    }

    private async Task<IGeminiResponse> ProcessNonSuccessResponseAsync(IGeminiResponse response, Uri uri,
        bool allowRepeat,
        int depth)
    {
        if (response.IsInputRequired && allowRepeat && response is InputRequiredResponse inputResponse)
        {
            // prompt the caller to provide input, and re-send the request if any ways provided
            var args = new InputRequiredArgs(inputResponse.Sensitive,
                inputResponse.Message ?? "Input required");
            if (InputRequiredCallback != null)
                await InputRequiredCallback(args);
            if (!string.IsNullOrEmpty(args.Value))
            {
                // caller provided input; re-send with the input
                var updatedUri = new UriBuilder(uri) { Query = args.Value }.Uri;
                return await SendUriRequestAsync(updatedUri, false);
            }
        }
        else if (response.IsRedirect && response is RedirectResponse redirectResponse)
        {
            if (_redirectBehavior == RedirectBehavior.Ignore)
                return response;

            if (depth >= MaxRedirectDepth)
                return new ErrorResponse(uri, StatusCode.Unknown, "Too many redirects");

            // prompt the caller to confirm redirection
            var args = new ConfirmRedirectArgs(redirectResponse.RedirectTo, redirectResponse.IsPermanent);

            var shouldFollow = true;
            if (_redirectBehavior == RedirectBehavior.Confirm && ConfirmRedirectCallback != null)
            {
                await ConfirmRedirectCallback(args);
                shouldFollow = args.FollowRedirect;
            }

            if (shouldFollow)
            {
                var nextUri = new Uri(redirectResponse.RedirectTo, UriKind.RelativeOrAbsolute);

                // redirects have to support relative URIs;
                if (!nextUri.IsAbsoluteUri)
                    nextUri = new UriBuilder(nextUri)
                        { Scheme = uri.Scheme, Host = uri.Host, Port = uri.IsDefaultPort ? -1 : uri.Port }.Uri;

                return await SendUriRequestAsync(nextUri, depth: depth + 1);
            }
        }

        return response;
    }

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

        // seek past the header + newline
        contents.Seek(header.LengthIncludingNewline, SeekOrigin.Begin);

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

    private async Task<bool> IsCertificateValidAsync(IClientCertificate cert)
    {
        if (DateTime.Now > cert.Certificate.NotAfter)
        {
            // certificate has expired; present the caller with the opportunity to renew it
            if (CertificateExpiredCallback != null)
            {
                var args = new CertificateExpiredArgs(cert);
                await CertificateExpiredCallback(args);

                if (args.Replacement != null)
                {
                    await AuthenticationDatabase.RemoveAsync(cert);
                    cert.Certificate = args.Replacement.Certificate;
                    await AuthenticationDatabase.AddAsync(cert, args.Password);
                    return true;
                }
            }

            return false;
        }

        return true;
    }

    private async Task<bool> CanSendCertificateAsync(IClientCertificate cert)
    {
        if (SendingClientCertificateCallback == null)
            return true;

        var args = new SendingClientCertificateArgs(cert);
        await SendingClientCertificateCallback(args);

        return !args.Cancel;
    }

    private static async Task SendRequestAsync(Uri uri, SslStream stream)
    {
        await stream.WriteAsync(ConvertToUtf8(uri.ToString()));
        await stream.WriteAsync(ConvertToUtf8("\r\n"));
        await stream.FlushAsync();
    }

    private static SslStream BuildSslStream(Uri uri, Func<Uri, X509Certificate, bool> validation)
    {
        var tcpClient = new TcpClient(uri.Host, uri.IsDefaultPort ? DefaultPort : uri.Port);
        return new SslStream(tcpClient.GetStream(), false,
            (_, cert, _, _) => validation(uri, cert),
            null, EncryptionPolicy.RequireEncryption);
    }

    private async Task<bool> ValidateCertificate(Uri uri, X509Certificate certificate)
    {
        if (CertificateDatabase.IsCertificateValid(uri.Host, certificate, out var result))
            return true;

        if (result == InvalidCertificateReason.TrustedMismatch && RemoteCertificateInvalidCallback != null)
        {
            var args = new RemoteCertificateUnrecognizedArgs(uri.Host,
                certificate.GetCertHashString(HashAlgorithmName.SHA256));
            await RemoteCertificateUnrecognizedCallback(args);

            if (!args.AcceptAndTrust)
                return false;

            // user opted to accept this certificate in place of the previously-recognized one,
            // so update the cache and re-validate
            CertificateDatabase.RemoveTrusted(uri.Host);
            return await ValidateCertificate(uri, certificate);
        }

        if (RemoteCertificateInvalidCallback != null)
            await RemoteCertificateInvalidCallback(new RemoteCertificateInvalidArgs(uri.Host, result));

        return false;
    }
}