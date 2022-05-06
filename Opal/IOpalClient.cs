using Opal.Authentication.Certificate;
using Opal.Event;
using Opal.Response;

namespace Opal;

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
    ///     Removes the client certificate metadata from the client's certificate database and deletes any associated resources
    /// </summary>
    void RemoveCertificate(IClientCertificate certificate);


    event EventHandler<InputRequiredEventArgs> InputRequired;
    event EventHandler<CertificateRequiredEventArgs> CertificateRequired;
    event EventHandler<ConfirmRedirectEventArgs> ConfirmRedirect;
}