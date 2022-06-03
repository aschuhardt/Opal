using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using Opal.Authentication.Certificate;

namespace Opal.Authentication.Database;

public class InMemoryAuthenticationDatabase : IAuthenticationDatabase
{
    protected readonly IDictionary<string, IClientCertificate> StoredCertificates;

    public InMemoryAuthenticationDatabase()
    {
        StoredCertificates =
            new ConcurrentDictionary<string, IClientCertificate>(StringComparer.InvariantCultureIgnoreCase);
    }

    public IEnumerable<IClientCertificate> Certificates => StoredCertificates.Values;

    public virtual IAsyncEnumerable<IClientCertificate> CertificatesAsync => throw new NotImplementedException();

    public virtual Task<IClientCertificate> TryGetCertificateAsync(string host)
    {
        return StoredCertificates.TryGetValue(host, out var cert)
            ? Task.FromResult(cert)
            : Task.FromResult<IClientCertificate>(null);
    }

    public virtual Task AddAsync(X509Certificate2 certificate, string password)
    {
        return AddAsync(new ClientCertificate(certificate), password);
    }

    public virtual Task AddAsync(IClientCertificate certificate, string password)
    {
        StoredCertificates[certificate.Fingerprint] = certificate;
        return Task.CompletedTask;
    }

    public virtual Task RemoveAsync(string fingerprint)
    {
        StoredCertificates.Remove(fingerprint);
        return Task.CompletedTask;
    }

    public virtual Task RemoveAsync(IClientCertificate certificate)
    {
        return RemoveAsync(certificate.Fingerprint);
    }

    public Func<string, Task<string>> PasswordEntryCallback { get; set; }
    public Func<string, Task> CertificateFailureCallback { get; set; }
}