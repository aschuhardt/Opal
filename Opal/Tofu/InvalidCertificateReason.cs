namespace Opal.Tofu
{
    public enum InvalidCertificateReason
    {
        /// <summary>
        ///     The request host does not match the name specified in the certificate
        /// </summary>
        NameMismatch,

        /// <summary>
        ///     The certificate differs from the previously trusted certificate
        /// </summary>
        TrustedMismatch,

        /// <summary>
        ///     The certificate has expired
        /// </summary>
        Expired,

        /// <summary>
        ///     The certificate is not yet valid
        /// </summary>
        NotYet,

        /// <summary>
        ///     The certificate is invalid for some other reason
        /// </summary>
        Other,

        /// <summary>
        ///     Certificate is missing a name
        /// </summary>
        MissingInformation
    }
}