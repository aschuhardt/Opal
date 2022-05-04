using System.Security.Cryptography.X509Certificates;

namespace Opal.Authentication;

public interface IAuthenticationDatabase
{
    bool TryGetCertificate(string host, out X509Certificate2 certificate);
    void Add(string host, X509Certificate2 certificate);
    void Remove(string host);
}