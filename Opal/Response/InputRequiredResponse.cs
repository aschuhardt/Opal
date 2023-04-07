using System;

namespace Opal.Response
{
    public class InputRequiredResponse : GeminiResponseBase
    {
        internal InputRequiredResponse(Uri uri, bool sensitive, string message) : base(sensitive
            ? StatusCode.InputSensitive
            : StatusCode.Input, uri)
        {
            Sensitive = sensitive;
            Message = message;
        }

        public bool Sensitive { get; }
        public string Message { get; }
    }
}