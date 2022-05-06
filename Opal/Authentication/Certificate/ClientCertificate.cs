using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Opal.Authentication.Certificate;

internal class ClientCertificate : IClientCertificate
{
    private readonly X509Certificate2 _certificate;

    public ClientCertificate(X509Certificate2 certificate, string host, string name)
    {
        _certificate = certificate;
        Host = host;
        Name = name;
    }

    X509Certificate2 IClientCertificate.Certificate => _certificate;

    public string Host { get; }
    public string Name { get; }
    public string Fingerprint => _certificate.GetCertHashString(HashAlgorithmName.SHA256);
}