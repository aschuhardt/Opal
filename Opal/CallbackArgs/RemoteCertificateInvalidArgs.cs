using Opal.Tofu;

namespace Opal.CallbackArgs
{
    public class RemoteCertificateInvalidArgs
    {
        internal RemoteCertificateInvalidArgs(string host, InvalidCertificateReason reason)
        {
            Host = host;
            Reason = reason;
        }

        public string Host { get; }

        public InvalidCertificateReason Reason { get; }
    }
}