namespace Opal.Event;

public class CertificatePasswordRequiredEventArgs
{
    internal CertificatePasswordRequiredEventArgs(string host, string name, string fingerprint)
    {
        Host = host;
        Name = name;
        Fingerprint = fingerprint;
    }

    public string Host { get; }
    public string Fingerprint { get; }
    public string Name { get; }
    public string Password { get; set; }
}