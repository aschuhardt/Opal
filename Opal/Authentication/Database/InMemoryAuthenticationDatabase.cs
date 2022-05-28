using System.Collections.Concurrent;
using Opal.Authentication.Certificate;
using Opal.Event;

namespace Opal.Authentication.Database;

public class InMemoryAuthenticationDatabase : IAuthenticationDatabase
{
    protected readonly IDictionary<string, IClientCertificate> StoredCertificates;

    public InMemoryAuthenticationDatabase()
    {
        StoredCertificates =
            new ConcurrentDictionary<string, IClientCertificate>(StringComparer.InvariantCultureIgnoreCase);
    }

    public virtual CertificateResult TryGetCertificate(string host, out IClientCertificate certificate)
    {
        return StoredCertificates.TryGetValue(host, out certificate)
            ? CertificateResult.Success
            : CertificateResult.Missing;
    }

    public virtual void Add(IClientCertificate certificate, string password)
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
        Remove(certificate.Host);
    }

    public IEnumerable<IClientCertificate> Certificates => StoredCertificates.Values;
    public virtual event EventHandler<CertificatePasswordRequiredEventArgs> CertificatePasswordRequired;
}