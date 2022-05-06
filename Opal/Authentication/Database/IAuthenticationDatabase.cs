using System.Security.Cryptography.X509Certificates;
using Opal.Authentication.Certificate;

namespace Opal.Authentication.Database;

public interface IAuthenticationDatabase
{
    bool TryGetCertificate(string host, out IClientCertificate certificate);
    void Add(IClientCertificate certificate);
    void Remove(string host);
    void Remove(IClientCertificate certificate);
    public IEnumerable<IClientCertificate> Certificates { get; }
}