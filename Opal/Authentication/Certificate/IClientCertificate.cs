using System.Security.Cryptography.X509Certificates;

namespace Opal.Authentication.Certificate;

public interface IClientCertificate
{
    internal X509Certificate2 Certificate { get; }
    public string Host { get; }
    public string Name { get; }
    public string Fingerprint { get; }
}