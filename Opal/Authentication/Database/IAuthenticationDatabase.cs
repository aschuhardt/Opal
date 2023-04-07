using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Opal.Authentication.Certificate;

namespace Opal.Authentication.Database
{
    public interface IAuthenticationDatabase
    {
        Func<string, Task<string>> PasswordEntryCallback { get; set; }
        Func<string, Task> CertificateFailureCallback { get; set; }
        Task<IClientCertificate> TryGetCertificateAsync(string fingerprint);
        Task AddAsync(IClientCertificate certificate);
        Task RemoveAsync(string fingerprint);
        Task RemoveAsync(IClientCertificate certificate);
        Task AddAsync(X509Certificate2 certificate);
    }
}