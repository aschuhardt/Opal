using System.Security.Cryptography.X509Certificates;

namespace Opal.Authentication.Certificate;

public interface IClientCertificate
{
    internal X509Certificate2 Certificate { get; set; }
    string Fingerprint { get; }
    public string Subject { get; }
    DateTime Expiration { get; }
    IClientCertificate Renew(TimeSpan lifespan);
}