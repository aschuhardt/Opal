using Opal.Tofu;

namespace Opal.Event;

public class RemoteCertificateInvalidEventArgs : EventArgs
{
    internal RemoteCertificateInvalidEventArgs(string host, InvalidCertificateReason reason)
    {
        Host = host;
        Reason = reason;
    }

    public string Host { get; }

    public InvalidCertificateReason Reason { get; }
}