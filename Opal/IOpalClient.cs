using System;
using System.Threading.Tasks;
using Opal.Authentication.Certificate;
using Opal.CallbackArgs;
using Opal.Response;
using Opal.Tofu;

namespace Opal
{
    /// <summary>
    ///     Describes the behavior of a Gemini client implementation
    /// </summary>
    public interface IOpalClient
    {
        /// <summary>
        ///     This will be used in order to validate incoming remote certificates.
        /// </summary>
        ICertificateDatabase CertificateDatabase { get; }

        /// <summary>
        ///     If assigned, this will be called in order to get the active client certificate.
        /// </summary>
        Func<Task<IClientCertificate>> GetActiveClientCertificateCallback { get; set; }

        /// <summary>
        ///     If assigned, this will be called if the active client certificate has expired.  You may use this method to prompt
        ///     the user to renew their certificate, for example.
        /// </summary>
        Func<CertificateExpiredArgs, Task> ClientCertificateIsExpiredCallback { get; set; }

        /// <summary>
        ///     If assigned and the redirect behavior was set to <see cref="RedirectBehavior.Confirm" />, this will be called when
        ///     a redirect response is received.  This is done in order to allow the application to specify whether or not to
        ///     follow that redirect.
        /// </summary>
        Func<ConfirmRedirectArgs, Task> ConfirmRedirectCallback { get; set; }

        /// <summary>
        ///     If assigned, this will be called when a code-1X response is received, in order to allow the application to provide
        ///     input arguments.  If an input value is provided, then initial request will be re-submitted.
        /// </summary>
        Func<InputRequiredArgs, Task> InputRequiredCallback { get; set; }

        /// <summary>
        ///     If assigned, this will be called before sending the active client certificate.  This can be used to interrupt that
        ///     process and prevent the certificate from being sent.
        /// </summary>
        Func<SendingClientCertificateArgs, Task> SendingClientCertificateCallback { get; set; }

        /// <summary>
        ///     If assigned, this will be called when a remote certificate has been found to be invalid (according to the current
        ///     <see cref="ICertificateDatabase" /> implementation).
        /// </summary>
        Func<RemoteCertificateInvalidArgs, Task> RemoteCertificateInvalidCallback { get; set; }

        /// <summary>
        ///     If assigned, this will be called when the current <see cref="ICertificateDatabase" /> indicates that a remote
        ///     certificate does not match the certificate that was previously stored for a given host.  Use this callback to
        ///     approve or deny the replacement certificate for the host.
        /// </summary>
        Func<RemoteCertificateUnrecognizedArgs, Task> RemoteCertificateUnrecognizedCallback { get; set; }

        Task<IGeminiResponse> SendRequestAsync(string uri);
        Task<IGeminiResponse> SendRequestAsync(string uri, string input);
        Task<IGeminiResponse> SendRequestAsync(Uri uri);
    }
}