using Opal.Authentication.Certificate;
using Opal.Authentication.Database;
using Opal.CallbackArgs;
using Opal.Tofu;

namespace Opal;

/// <summary>
///     Describes the behavior of a Gemini client implementation
/// </summary>
public interface IOpalClient
{
    IAuthenticationDatabase AuthenticationDatabase { get; }
    ICertificateDatabase CertificateDatabase { get; }
    Func<Task<IClientCertificate>> GetActiveCertificateCallback { get; set; }
    Func<CertificateExpiredArgs, Task> CertificateExpiredCallback { get; set; }
    Func<ConfirmRedirectArgs, Task> ConfirmRedirectCallback { get; set; }
    Func<InputRequiredArgs, Task> InputRequiredCallback { get; set; }
    Func<SendingClientCertificateArgs, Task> SendingClientCertificateCallback { get; set; }
    Func<RemoteCertificateInvalidArgs, Task> RemoteCertificateInvalidCallback { get; set; }
    Func<RemoteCertificateUnrecognizedArgs, Task> RemoteCertificateUnrecognizedCallback { get; set; }
    Task<IGeminiResponse> SendRequestAsync(string uri);
    Task<IGeminiResponse> SendRequestAsync(string uri, string input);
}