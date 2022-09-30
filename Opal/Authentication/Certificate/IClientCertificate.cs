using System.Security.Cryptography.X509Certificates;

namespace Opal.Authentication.Certificate;

public interface IClientCertificate
{
    public X509Certificate2 Certificate { get; internal set; }
    string Fingerprint { get; }
    public string Subject { get; }
    DateTime Expiration { get; }
    IClientCertificate Renew(TimeSpan lifespan);
}