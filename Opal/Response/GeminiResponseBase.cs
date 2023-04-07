using System;

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

        public override string ToString()
        {
            return $"{(int)Status} {Status}";
        }
    }
}