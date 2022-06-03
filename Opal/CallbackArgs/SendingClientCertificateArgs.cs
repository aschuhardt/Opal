using Opal.Authentication.Certificate;

namespace Opal.CallbackArgs;

public class SendingClientCertificateArgs
{
    public SendingClientCertificateArgs(IClientCertificate certificate)
    {
        Certificate = certificate;
    }

    public IClientCertificate Certificate { get; }
    public bool Cancel { get; set; }
}