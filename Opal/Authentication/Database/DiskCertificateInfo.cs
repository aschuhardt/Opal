namespace Opal.Authentication.Database;

internal class DiskCertificateInfo
{
    public string Host { get; set; }
    public string Path { get; set; }
    public string Name { get; set; }
    public string Fingerprint { get; set; }
    public bool Encrypted { get; set; }
}