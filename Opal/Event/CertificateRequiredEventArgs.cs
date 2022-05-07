using System.Security.Cryptography.X509Certificates;

namespace Opal.Event;

public class CertificateRequiredEventArgs : EventArgs
{
    internal CertificateRequiredEventArgs(string message, string host)
    {
        Message = message;
        Host = host;
    }

    public string Message { get; }
    public string Host { get; }
    public X509Certificate2 Certificate { get; set; }
    public string Password { get; set; }
}