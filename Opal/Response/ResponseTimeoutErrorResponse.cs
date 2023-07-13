using System;

namespace Opal.Response
{
    internal class ResponseTimeoutErrorResponse : ErrorResponse
    {
        internal ResponseTimeoutErrorResponse(Uri uri)
            : base(uri, StatusCode.Unknown, "The server took too long to respond")
        {
        }
    }
}