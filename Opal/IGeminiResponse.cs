using System;
using Opal.Response;

namespace Opal
{
    public interface IGeminiResponse
    {
        StatusCode Status { get; }

        Uri Uri { get; }

        bool IsSuccess { get; }

        bool IsInputRequired { get; }

        bool IsRedirect { get; }

        bool IsTemporaryFailure { get; }

        bool IsPermanentFailure { get; }

        bool IsCertificateRequired { get; }

        bool IsCertificateRejected { get; }

        bool IsInvalid { get; }
    }
}