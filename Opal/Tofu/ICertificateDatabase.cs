using System.Security.Cryptography.X509Certificates;

namespace Opal.Tofu
{
    public interface ICertificateDatabase
    {
        /// <summary>
        ///     Checks whether the provided certificate is valid for the host and can be trusted
        /// </summary>
        /// <param name="host">The host to which the request was sent</param>
        /// <param name="certificate">The X.509v3 certificate that the server responded with</param>
        /// <param name="result">A valid indicating the result of the validation (i.e. the reason for failure)</param>
        /// <returns>True if the certificate is valid and trusted, otherwise false</returns>
        bool IsCertificateValid(string host, X509Certificate certificate, out InvalidCertificateReason result);
    }
}