using System.Security.Cryptography.X509Certificates;

namespace Opal.Tofu
{
    public class DummyCertificateDatabase : ICertificateDatabase
    {
        public virtual bool IsCertificateValid(string host, X509Certificate certificate,
            out InvalidCertificateReason result)
        {
            result = default;
            return true;
        }
    }
}