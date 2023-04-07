using System;
using System.Security.Cryptography.X509Certificates;

#if NETSTANDARD2_0
#else
using System.Security.Cryptography;
#endif

namespace Opal.Authentication.Certificate
{
    public class ClientCertificate : IClientCertificate
    {
        public ClientCertificate(X509Certificate2 certificate)
        {
            ((IClientCertificate)this).Certificate = certificate;
            Subject = certificate.Subject;
            Expiration = certificate.NotAfter;
        }

        X509Certificate2 IClientCertificate.Certificate { get; set; }

#if NETSTANDARD2_0
        public string Fingerprint => ((IClientCertificate)this).Certificate.GetCertHashString();
#else
        public string Fingerprint => ((IClientCertificate)this).Certificate.GetCertHashString(HashAlgorithmName.SHA256);
#endif
        public string Subject { get; }
        public DateTime Expiration { get; }

        public IClientCertificate Renew(TimeSpan lifespan)
        {
            return new ClientCertificate(CertificateHelper.Renew(lifespan, ((IClientCertificate)this).Certificate));
        }
    }
}