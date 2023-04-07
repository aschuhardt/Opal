using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Opal.Authentication.Certificate;

namespace Opal.Authentication.Database
{
    public class InMemoryAuthenticationDatabase : IAuthenticationDatabase
    {
        protected readonly IDictionary<string, IClientCertificate> StoredCertificates;

        public InMemoryAuthenticationDatabase()
        {
            StoredCertificates =
                new ConcurrentDictionary<string, IClientCertificate>(StringComparer.InvariantCultureIgnoreCase);
        }

        public IEnumerable<IClientCertificate> Certificates => StoredCertificates.Values;

        public virtual Task<IClientCertificate> TryGetCertificateAsync(string host)
        {
            return StoredCertificates.TryGetValue(host, out var cert)
                ? Task.FromResult(cert)
                : Task.FromResult<IClientCertificate>(null);
        }

        public virtual Task AddAsync(X509Certificate2 certificate)
        {
            return AddAsync(new ClientCertificate(certificate));
        }

        public virtual Task AddAsync(IClientCertificate certificate)
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
}