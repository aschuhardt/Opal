namespace Opal.Event;

public class RemoteCertificateUnrecognizedEventArgs : EventArgs
{
    internal RemoteCertificateUnrecognizedEventArgs(string host, string fingerprint)
    {
        Host = host;
        Fingerprint = fingerprint;
        AcceptAndTrust = false;
    }

    public string Host { get; }

    public string Fingerprint { get; }

    public bool AcceptAndTrust { get; set; }
}