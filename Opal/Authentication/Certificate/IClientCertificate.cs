using System;
using System.Security.Cryptography.X509Certificates;

namespace Opal.Authentication.Certificate
{
    public interface IClientCertificate
    {
        X509Certificate2 Certificate { get; set; }
        string Fingerprint { get; }
        string Subject { get; }
        DateTime Expiration { get; }
        IClientCertificate Renew(TimeSpan lifespan);
    }
}