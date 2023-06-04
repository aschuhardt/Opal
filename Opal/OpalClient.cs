using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Opal.Authentication.Certificate;
using Opal.CallbackArgs;
using Opal.Header;
using Opal.Response;
using Opal.Tofu;

#if NETSTANDARD2_0
#else
using System.Security.Cryptography;
#endif

namespace Opal
{
    public class OpalClient : IOpalClient
    {
        private const int MaxRedirectDepth = 5;
        private const int DefaultPort = 1965;
        private const int Timeout = 4000;
        private const string Scheme = "gemini";
        private static readonly string SchemePrefix = $"{Scheme}://";
        private readonly RedirectBehavior _redirectBehavior;

        public OpalClient() : this(new DummyCertificateDatabase(), RedirectBehavior.Follow)
        {
        }

        public OpalClient(ICertificateDatabase certificateDatabase, RedirectBehavior redirectBehavior)
        {
            if (!UriParser.IsKnownScheme("gemini"))
            {
                UriParser.Register(
                    new GenericUriParser(GenericUriParserOptions.NoFragment | GenericUriParserOptions.Default),
                    Scheme, DefaultPort);
            }

            CertificateDatabase = certificateDatabase;
            _redirectBehavior = redirectBehavior;
        }

        /// <inheritdoc />
        public ICertificateDatabase CertificateDatabase { get; }

        /// <inheritdoc />
        public Func<Task<IClientCertificate>> GetActiveClientCertificateCallback { get; set; }

        /// <inheritdoc />
        public Func<CertificateExpiredArgs, Task> ClientCertificateIsExpiredCallback { get; set; }

        /// <inheritdoc />
        public Func<ConfirmRedirectArgs, Task> ConfirmRedirectCallback { get; set; }

        /// <inheritdoc />
        public Func<InputRequiredArgs, Task> InputRequiredCallback { get; set; }

        /// <inheritdoc />
        public Func<SendingClientCertificateArgs, Task> SendingClientCertificateCallback { get; set; }

        /// <inheritdoc />
        public Func<RemoteCertificateInvalidArgs, Task> RemoteCertificateInvalidCallback { get; set; }

        /// <inheritdoc />
        public Func<RemoteCertificateUnrecognizedArgs, Task> RemoteCertificateUnrecognizedCallback { get; set; }

        public Task<IGeminiResponse> SendRequestAsync(Uri uri)
        {
            return SendUriRequestAsync(uri);
        }

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

#if NETSTANDARD2_0
                using (var stream = BuildSslStream(uri, CertificateValidationCallback))
#else
                await using (var stream = BuildSslStream(uri, CertificateValidationCallback))
#endif
                {
                    stream.ReadTimeout = Timeout;
                    stream.WriteTimeout = Timeout;

                    IClientCertificate cert = null;

                    if (GetActiveClientCertificateCallback != null)
                        cert = await GetActiveClientCertificateCallback();

                    // authenticate
                    var hasValidCert = cert != null && await IsCertificateValidAsync(cert) &&
                                       await CanSendCertificateAsync(cert);

#if NETSTANDARD2_0
                    await stream.AuthenticateAsClientAsync(uri.Host,
                        hasValidCert ? new X509Certificate2Collection(cert.Certificate) : null, SslProtocols.Tls12,
                        false);
#else
                    await stream.AuthenticateAsClientAsync(uri.Host,
                        hasValidCert ? new X509Certificate2Collection(cert.Certificate) : null,
                        SslProtocols.Tls12 | SslProtocols.Tls13, false);
#endif

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
            catch (AuthenticationException e)
            {
                return new RemoteCertificateErrorResponse(uri, e);
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

            var reader = new StreamReader(body);
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
                    if (!nextUri.IsAbsoluteUri && !Uri.TryCreate(uri, redirectResponse.RedirectTo, out nextUri))
                        return new ErrorResponse(uri, StatusCode.Unknown, "Invalid redirect URI");

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
#if NETSTANDARD2_0
            return mimetype.ToUpperInvariant().Contains(GemtextResponse.GemtextMimeType);
#else
        return mimetype.Contains(GemtextResponse.GemtextMimeType, StringComparison.InvariantCultureIgnoreCase);
#endif
        }

        private static IGeminiResponse BuildResponse(Uri uri, GeminiHeader header, Stream contents)
        {
            // make sure the status code is recognizable
            if (!Enum.IsDefined(typeof(StatusCode), header.StatusCode))
                return new InvalidResponse(uri);

            // seek past the header + newline
            contents.Seek(header.LengthIncludingNewline, SeekOrigin.Begin);

            var status = (StatusCode)header.StatusCode;
            switch (status)
            {
                case StatusCode.Input:
                    return new InputRequiredResponse(uri, false, header.Meta);
                case StatusCode.InputSensitive:
                    return new InputRequiredResponse(uri, true, header.Meta);
                case StatusCode.Success:
                    return IsGemtextMimetype(header.Meta)
                        ? new GemtextResponse(uri, contents, header.Languages)
                        : new SuccessfulResponse(uri, contents, header.Meta);
                case StatusCode.TemporaryRedirect:
                    return new RedirectResponse(uri, false, header.Meta);
                case StatusCode.PermanentRedirect:
                    return new RedirectResponse(uri, true, header.Meta);
                case StatusCode.ClientCertificateRequired:
                case StatusCode.TemporaryFailure:
                case StatusCode.ServerUnavailable:
                case StatusCode.CgiError:
                case StatusCode.ProxyError:
                case StatusCode.SlowDown:
                case StatusCode.PermanentFailure:
                case StatusCode.NotFound:
                case StatusCode.Gone:
                case StatusCode.ProxyRequestRefused:
                case StatusCode.BadRequest:
                case StatusCode.CertificateNotAuthorized:
                case StatusCode.CertificateNotValid:
                    return new ErrorResponse(uri, status, header.Meta);
                case StatusCode.Unknown:
                default:
                    return new InvalidResponse(uri);
            }
        }

        private async Task<bool> IsCertificateValidAsync(IClientCertificate cert)
        {
            if (DateTime.Now > cert.Certificate.NotAfter)
            {
                // certificate has expired; present the caller with the opportunity to renew it
                if (ClientCertificateIsExpiredCallback != null)
                {
                    var args = new CertificateExpiredArgs(cert);
                    await ClientCertificateIsExpiredCallback(args);

                    if (args.Replacement != null)
                    {
                        cert.Certificate = args.Replacement.Certificate;
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
            var request = ConvertToUtf8($"{uri}\r\n");
            await stream.WriteAsync(request, 0, request.Length);
        }

        private static SslStream BuildSslStream(Uri uri, Func<Uri, X509Certificate, bool> validation)
        {
            var tcpClient = new TcpClient(uri.Host, uri.IsDefaultPort ? DefaultPort : uri.Port);
            return new SslStream(tcpClient.GetStream(), false,
                (sender, certificate, chain, errors) => validation(uri, certificate),
                null, EncryptionPolicy.RequireEncryption);
        }

        private async Task<bool> ValidateCertificate(Uri uri, X509Certificate certificate)
        {
            if (CertificateDatabase.IsCertificateValid(uri.Host, certificate, out var result))
                return true;

            if (result == InvalidCertificateReason.TrustedMismatch && RemoteCertificateInvalidCallback != null)
            {
#if NETSTANDARD2_0
                var hash = certificate.GetCertHashString();
#else
                var hash = certificate.GetCertHashString(HashAlgorithmName.SHA256);
#endif
                var args = new RemoteCertificateUnrecognizedArgs(uri.Host,
                    hash);
                await RemoteCertificateUnrecognizedCallback(args);

                return args.AcceptAndTrust;
            }

            if (RemoteCertificateInvalidCallback != null)
                await RemoteCertificateInvalidCallback(new RemoteCertificateInvalidArgs(uri.Host, result));

            return false;
        }
    }
}