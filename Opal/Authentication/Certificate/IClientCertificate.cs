using System.Security.Cryptography.X509Certificates;

namespace Opal.Authentication.Certificate;

public interface IClientCertificate
{
    internal X509Certificate2 Certificate { get; }
    string Host { get; }
    string Name { get; }
    string Fingerprint { get; }
    IClientCertificate Renew(TimeSpan lifespan);
}