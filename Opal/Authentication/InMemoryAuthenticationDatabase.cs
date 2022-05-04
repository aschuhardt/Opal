using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace Opal.Authentication;

internal class InMemoryAuthenticationDatabase : IAuthenticationDatabase
{
    protected readonly IDictionary<string, X509Certificate2> Certificates;

    public InMemoryAuthenticationDatabase()
    {
        Certificates = new ConcurrentDictionary<string, X509Certificate2>(StringComparer.InvariantCultureIgnoreCase);
    }

    public virtual bool TryGetCertificate(string host, out X509Certificate2 certificate)
    {
        return Certificates.TryGetValue(host, out certificate);
    }

    public virtual void Add(string host, X509Certificate2 certificate)
    {
        Certificates[host] = certificate;
    }

    public virtual void Remove(string host)
    {
        if (Certificates.ContainsKey(host)) Certificates.Remove(host);
    }
}