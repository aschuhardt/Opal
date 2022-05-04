using Opal.Response;

namespace Opal;

public interface IGeminiResponse
{
    public StatusCode Status { get; }

    public Uri Uri { get; }

    public bool IsSuccess => Status == StatusCode.Success;

    public bool IsInputRequired => Status is StatusCode.Input or StatusCode.InputSensitive;

    public bool IsRedirect => Status is StatusCode.PermanentRedirect or StatusCode.TemporaryRedirect;

    public bool IsTemporaryFailure => Status is StatusCode.TemporaryFailure or StatusCode.ServerUnavailable
        or StatusCode.CgiError or StatusCode.ProxyError or StatusCode.SlowDown;

    public bool IsPermanentFailure => Status is StatusCode.PermanentFailure or StatusCode.NotFound
        or StatusCode.Gone or StatusCode.ProxyRequestRefused or StatusCode.BadRequest;

    public bool IsCertificateRequired => Status is StatusCode.ClientCertificateRequired;

    public bool IsCertificateRejected =>
        Status is StatusCode.CertificateNotAuthorized or StatusCode.CertificateNotValid;

    public bool IsInvalid => Status is StatusCode.Unknown;
}