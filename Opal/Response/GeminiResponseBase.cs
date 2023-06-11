using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Opal.Response
{
    public abstract class GeminiResponseBase : IGeminiResponse
    {
        protected GeminiResponseBase(StatusCode status, Uri uri)
        {
            Status = status;
            Uri = uri;
        }

        public StatusCode Status { get; }
        public Uri Uri { get; }
        public bool IsSuccess => Status == StatusCode.Success;
        public bool IsInputRequired => Status == StatusCode.Input || Status == StatusCode.InputSensitive;
        public bool IsRedirect => Status == StatusCode.PermanentRedirect || Status == StatusCode.TemporaryRedirect;

        public bool IsTemporaryFailure =>
            Status == StatusCode.TemporaryFailure ||
            Status == StatusCode.ServerUnavailable || Status == StatusCode.CgiError ||
            Status == StatusCode.ProxyError || Status == StatusCode.SlowDown;

        public bool IsPermanentFailure =>
            Status == StatusCode.PermanentFailure || Status == StatusCode.NotFound ||
            Status == StatusCode.Gone || Status == StatusCode.ProxyRequestRefused ||
            Status == StatusCode.BadRequest;

        public bool IsCertificateRequired => Status is StatusCode.ClientCertificateRequired;
        public bool IsCertificateRejected => Status == StatusCode.CertificateNotAuthorized || Status == StatusCode.CertificateNotValid;
        public bool IsInvalid => Status is StatusCode.Unknown;

        public X509Certificate HostCertificate { get; private set; }
        public SslProtocols Protocol { get; private set; }
        public CipherAlgorithmType CipherAlgorithm { get; private set; }
        public HashAlgorithmType HashAlgorithm { get; private set; }
        public ExchangeAlgorithmType KeyExchangeAlgorithm { get; private set; }

        public override string ToString()
        {
            return $"{(int)Status} {Status}";
        }

        internal void EnrichWithSslStreamMetadata(SslStream stream)
        {
            if (stream == null)
                return;

            HostCertificate = stream.RemoteCertificate;
            Protocol = stream.SslProtocol;
            CipherAlgorithm = stream.CipherAlgorithm;
            HashAlgorithm = stream.HashAlgorithm;
            KeyExchangeAlgorithm = stream.KeyExchangeAlgorithm;
        }
    }
}