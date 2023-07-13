using System;

namespace Opal.Response
{
    internal class GeneralErrorResponse : ErrorResponse
    {
        public GeneralErrorResponse(Uri uri, Exception ex) : base(uri,
            StatusCode.Unknown, ex?.InnerException?.Message ?? ex?.Message ?? "An unhandled exception was thrown")
        {
        }
    }
}