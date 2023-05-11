using System;

namespace Opal.Response
{
    public class ErrorResponse : GeminiResponseBase
    {
        internal ErrorResponse(Uri uri, StatusCode status, string message) : base(status, uri)
        {
            Message = message;
            CanRetry = true; // generally okay to try again
        }

        public virtual bool CanRetry { get; }

        public string Message { get; }

        public override string ToString()
        {
            return $"{base.ToString()} \"{Message}\"";
        }
    }
}