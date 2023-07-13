using System;

namespace Opal.Response
{
    internal class EmptyErrorResponse : ErrorResponse
    {
        internal EmptyErrorResponse(Uri uri) 
            : base(uri, StatusCode.Unknown, "Received an empty response")
        {
        }
    }
}