using System;

namespace Opal.Response
{
    internal class RemoteCertificateErrorResponse : GeneralErrorResponse
    {
        public RemoteCertificateErrorResponse(Uri uri, Exception ex) : base(uri, ex)
        {
        }

        public override bool CanRetry => false; // the certificate was bad; re-sending the request is pointless
    }
}