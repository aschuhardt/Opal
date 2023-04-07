using System;
using System.Collections.Generic;
using System.IO;
using Opal.Document;
using Opal.Document.Line;

namespace Opal.Response
{
    public class GemtextResponse : SuccessfulResponse
    {
        internal const string GemtextMimeType = "text/gemini";
        private readonly IGemtextDocumentParser _parser;

        internal GemtextResponse(Uri uri, Stream body, IEnumerable<string> languages) : base(body, uri, GemtextMimeType,
            true)
        {
            _parser = new GemtextDocumentParser(uri);
            Languages = languages;
        }

        public IEnumerable<string> Languages { get; }

        public IEnumerable<ILine> AsDocument()
        {
            return _parser.ParseDocument(Body);
        }
    }
}