using System;

namespace Opal.Response
{
    internal class InvalidResponse : GeminiResponseBase
    {
        internal InvalidResponse(Uri uri) : base(StatusCode.Unknown, uri)
        {
        }
    }
}