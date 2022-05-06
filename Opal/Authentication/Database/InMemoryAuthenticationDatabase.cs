using System.Collections.Concurrent;
using Opal.Authentication.Certificate;

namespace Opal.Authentication.Database;

internal class InMemoryAuthenticationDatabase : IAuthenticationDatabase
{
    protected readonly IDictionary<string, IClientCertificate> StoredCertificates;

    public InMemoryAuthenticationDatabase()
    {
        StoredCertificates =
            new ConcurrentDictionary<string, IClientCertificate>(StringComparer.InvariantCultureIgnoreCase);
    }

    public virtual bool TryGetCertificate(string host, out IClientCertificate certificate)
    {
        return StoredCertificates.TryGetValue(host, out certificate);
    }

    public virtual void Add(IClientCertificate certificate)
    {
        StoredCertificates[certificate.Host] = certificate;
    }

    public virtual void Remove(string host)
    {
        if (StoredCertificates.ContainsKey(host))
            StoredCertificates.Remove(host);
    }

    public void Remove(IClientCertificate certificate)
    {
        if (StoredCertificates.ContainsKey(certificate.Host))
            StoredCertificates.Remove(certificate.Host);
    }

    public IEnumerable<IClientCertificate> Certificates => StoredCertificates.Values;
}