using System;
using System.IO;

namespace Opal.Response
{
    public class SuccessfulResponse : GeminiResponseBase
    {
        internal SuccessfulResponse(Uri uri, Stream body, string mimeType) : base(StatusCode.Success, uri)
        {
            Body = body;
            MimeType = mimeType;
            IsGemtext = false;
        }

        protected SuccessfulResponse(Stream body, Uri uri, string mimeType, bool isGemtext)
            : this(uri, body, mimeType)
        {
            IsGemtext = isGemtext;
        }

        public string MimeType { get; }
        public Stream Body { get; }
        public bool IsGemtext { get; }
    }
}