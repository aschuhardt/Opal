using Opal.Authentication.Certificate;
using Opal.Event;

namespace Opal.Authentication.Database;

public interface IAuthenticationDatabase
{
    CertificateResult TryGetCertificate(string host, out IClientCertificate certificate);
    void Add(IClientCertificate certificate, string password);
    void Remove(string host);
    void Remove(IClientCertificate certificate);
    public IEnumerable<IClientCertificate> Certificates { get; }
    public event EventHandler<CertificatePasswordRequiredEventArgs> CertificatePasswordRequired;
}