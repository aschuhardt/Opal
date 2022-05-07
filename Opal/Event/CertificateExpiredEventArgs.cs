using Opal.Authentication.Certificate;

namespace Opal.Event;

public class CertificateExpiredEventArgs
{
    public CertificateExpiredEventArgs(IClientCertificate existing)
    {
        Existing = existing;
    }

    public IClientCertificate Replacement { get; set; }
    public string Password { get; set; }
    public IClientCertificate Existing { get; }
}