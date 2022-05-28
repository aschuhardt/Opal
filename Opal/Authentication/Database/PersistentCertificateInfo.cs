namespace Opal.Authentication.Database;

public class PersistentCertificateInfo
{
    public string Host { get; set; }
    public string Path { get; set; }
    public string Name { get; set; }
    public string Fingerprint { get; set; }
    public bool Encrypted { get; set; }
}