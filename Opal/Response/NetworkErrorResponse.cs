using System;
using System.Net.Sockets;

namespace Opal.Response
{
    internal class NetworkErrorResponse : ErrorResponse
    {
        internal NetworkErrorResponse(Uri uri, SocketException ex) : base(uri, StatusCode.Unknown,
            $"{ex.Message} ({ex.SocketErrorCode})")
        {
        }
    }
}