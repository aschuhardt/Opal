using Opal.Authentication.Certificate;

namespace Opal.CallbackArgs
{
    public class CertificateExpiredArgs
    {
        public CertificateExpiredArgs(IClientCertificate existing)
        {
            Existing = existing;
        }

        public IClientCertificate Replacement { get; set; }
        public string Password { get; set; }
        public IClientCertificate Existing { get; }
    }
}