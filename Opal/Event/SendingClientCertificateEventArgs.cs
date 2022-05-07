using Opal.Authentication.Certificate;

namespace Opal.Event;

public class SendingClientCertificateEventArgs
{
    public SendingClientCertificateEventArgs(IClientCertificate certificate)
    {
        Certificate = certificate;
    }

    public IClientCertificate Certificate { get; }
    public bool Cancel { get; set; }
}