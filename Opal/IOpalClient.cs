using Opal.Authentication;
using Opal.Authentication.Certificate;
using Opal.Event;
using Opal.Response;

namespace Opal;

/// <summary>
///     Describes the behavior of a Gemini client implementation
/// </summary>
public interface IOpalClient
{
    /// <summary>
    ///     All of the client certificates currently available to the client for authentication purposes
    /// </summary>
    IEnumerable<IClientCertificate> Certificates { get; }

    /// <summary>
    ///     Sends a request to the provided relative or absolute URI
    /// </summary>
    /// <param name="uri">An absolute or relative Gemini URI</param>
    /// <returns>
    ///     A response object derived from <see cref="IGeminiResponse" /> representing the response received from the
    ///     server, or an instance of <see cref="NetworkErrorResponse" /> if no response was received
    /// </returns>
    IGeminiResponse SendRequest(string uri);

    /// <inheritdoc cref="SendRequest(string)" />
    /// <param name="uri">An absolute or relative Gemini URI</param>
    /// <param name="input">A string of input data to include in the request</param>
    IGeminiResponse SendRequest(string uri, string input);

    /// <summary>
    ///     Raised when a certificate provided by a server is invalid, describing why it is invalid
    /// </summary>
    event EventHandler<RemoteCertificateInvalidEventArgs> RemoteCertificateInvalid;

    /// <summary>
    ///     Raised when a server certificate is unrecognized; offers information useful for a user to determine and confirm
    ///     whether they want to trust that certificate
    /// </summary>
    event EventHandler<RemoteCertificateUnrecognizedEventArgs> RemoteCertificateUnrecognized;

    /// <summary>
    ///     Raised before attempting to load an encrypted client certificate
    /// </summary>
    public event EventHandler<CertificatePasswordRequiredEventArgs> CertificatePasswordRequired;

    /// <summary>
    ///     Removes the client certificate metadata from the client's certificate database and deletes any associated resources
    /// </summary>
    void RemoveCertificate(IClientCertificate certificate);

    /// <summary>
    ///     Raised when a server has indicated that user input is required; if input is provided via
    ///     <see cref="InputRequiredEventArgs.Value" />, then the initial request is re-sent with that value as a query
    ///     parameter
    /// </summary>
    event EventHandler<InputRequiredEventArgs> InputRequired;

    /// <summary>
    ///     Raised when a server has indicated that a client certificate is required, and none is found to exist in the
    ///     authentication database.  Use <see cref="CertificateHelper" /> in order to generate a new client certificate to
    ///     store in <see cref="CertificateRequiredEventArgs.Certificate" />.
    ///     <seealso cref="CertificateHelper.GenerateNew" />
    /// </summary>
    event EventHandler<CertificateRequiredEventArgs> CertificateRequired;

    /// <summary>
    ///     Raised prior to following a redirect if <see cref="RedirectBehavior.Confirm" /> was specified on
    ///     <see cref="OpalOptions" />
    ///     <seealso cref="RedirectBehavior.Confirm" />
    /// </summary>
    event EventHandler<ConfirmRedirectEventArgs> ConfirmRedirect;

    /// <summary>
    ///     Raised prior to sending a client certificate to the server.  Set
    ///     <see cref="SendingClientCertificateEventArgs.Cancel" /> to true in order to prevent the certificate from being
    ///     sent.
    /// </summary>
    event EventHandler<SendingClientCertificateEventArgs> SendingClientCertificate;

    /// <summary>
    ///     Raised prior to sending a client certificate to the server if that certificate is found to have been expired.  Set
    ///     <see cref="CertificateExpiredEventArgs.Replacement" /> to a new certificate instance if you'd like to use that one
    ///     instead.  Otherwise no action will be taken and the request will proceed as usual. Set
    ///     <see cref="CertificateExpiredEventArgs.Password" /> in order to encrypt the certificate's private key.
    ///     <seealso cref="CertificateHelper.GenerateNew" />
    /// </summary>
    event EventHandler<CertificateExpiredEventArgs> CertificateExpired;
}