namespace Opal.Authentication.Database;

public class PersistentCertificateInfo
{
    public string Path { get; set; }
    public string Fingerprint { get; set; }
    public bool Encrypted { get; set; }
}