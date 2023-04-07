using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Opal.Document.Line;

namespace Opal.Document
{
    /// <summary>
    ///     The default implementation of a Gemtext document parser.
    /// </summary>
    public class GemtextDocumentParser : IGemtextDocumentParser
    {
        private const string LinePrefixLink = "=>";
        private const string LinePrefixFormattedToggle = "```";
        private const string LinePrefixHeading1 = "#";
        private const string LinePrefixHeading2 = "##";
        private const string LinePrefixHeading3 = "###";
        private const string LinePrefixList = "*";
        private const string LinePrefixQuote = ">";

        private static readonly char[] WhitespaceChars = { ' ', '\t' };
        private readonly Uri _uri;

        public GemtextDocumentParser(Uri uri)
        {
            _uri = uri;
        }

        public IEnumerable<ILine> ParseDocument(Stream body)
        {
            if (!body.CanRead)
                yield break;

            using (var reader = new StreamReader(body))
            {
                var formatted = false;
                while (!reader.EndOfStream)
                    yield return CreateDocumentLine(reader.ReadLine(), ref formatted);
            }
        }

        private ILine CreateDocumentLine(string line, ref bool formatted)
        {
            if (string.IsNullOrWhiteSpace(line))
                return new EmptyLine();

            var firstChar = line[0];

            if (formatted && firstChar != '`')
                return new FormattedLine(line);

            switch (line)
            {
                case var _ when line.StartsWith(LinePrefixLink):
                    return ParseLinkLinePrefix(line);
                case var _ when line.StartsWith(LinePrefixFormattedToggle) && !formatted:
                    return ParsePrefixLineAndToggle(line, ref formatted, text => new FormattedBeginLine(text));
                case var _ when line.StartsWith(LinePrefixFormattedToggle) && formatted:
                    return ParsePrefixLineAndToggle(line, ref formatted, _ => new FormattedEndLine());
                case var _ when line.StartsWith(LinePrefixHeading3):
                    return ParsePrefixLine(line, LinePrefixHeading3, text => new HeadingLine(text, 3));
                case var _ when line.StartsWith(LinePrefixHeading2):
                    return ParsePrefixLine(line, LinePrefixHeading2, text => new HeadingLine(text, 2));
                case var _ when line.StartsWith(LinePrefixHeading1):
                    return ParsePrefixLine(line, LinePrefixHeading1, text => new HeadingLine(text, 1));
                case var _ when line.StartsWith(LinePrefixQuote):
                    return ParsePrefixLine(line, LinePrefixQuote, text => new QuoteLine(text));
                case var _ when line.StartsWith(LinePrefixList):
                    return ParsePrefixLine(line, LinePrefixList, text => new ListLine(text));
                case var _ when string.IsNullOrWhiteSpace(line):
                    return new EmptyLine();
                default:
                    return new TextLine(line);
            }
        }

        private static bool ContainsWhitespace(string value)
        {
            if (value.IndexOfAny(WhitespaceChars) >= 0)
                return true;

            return false;
        }

        private static ILine ParsePrefixLineAndToggle<T>(string line, ref bool flag, Func<string, T> create)
            where T : ILine
        {
            flag = !flag;
            return ContainsWhitespace(line)
                ? create(line.Substring(line.IndexOfAny(WhitespaceChars)).Trim())
                : create(null);
        }

        private static ILine ParsePrefixLine<T>(string line, string prefix, Func<string, T> create) where T : ILine
        {
            var text = line.Substring(prefix.Length);
            return string.IsNullOrWhiteSpace(text)
                ? (ILine)new TextLine(line)
                : create(text.Trim());
        }

        private ILine ParseLinkLinePrefix(string line)
        {
            var trimmedAfterPrefix = line.Substring(LinePrefixLink.Length).Trim();
            if (string.IsNullOrWhiteSpace(trimmedAfterPrefix))
                return new TextLine(line);

            var parts = trimmedAfterPrefix.Split(WhitespaceChars, 2, StringSplitOptions.RemoveEmptyEntries);

            var rawUri = new StringBuilder(parts[0]);

            // for protocol-relative URIs, prepend the scheme from the request
            if (parts[0].StartsWith("//"))
                rawUri.Insert(0, ':').Insert(0, _uri.Scheme);

            if (!Uri.TryCreate(rawUri.ToString(), UriKind.RelativeOrAbsolute, out var parsed))
                return new TextLine(line.Trim());

            if (!parsed.IsAbsoluteUri)
            {
                var pathParts = parts[0].Split(new[] { '?' }, StringSplitOptions.RemoveEmptyEntries);

                var linePath = pathParts[0];
                parsed = new UriBuilder
                {
                    Host = _uri.Host, Scheme = _uri.Scheme, Port = _uri.IsDefaultPort ? -1 : _uri.Port,
                    Path = BuildRelativePath(linePath, _uri),
                    Query = pathParts.Length > 1 ? pathParts[1] : string.Empty
                }.Uri;
            }

            return new LinkLine(parts.Length > 1 ? parts[1] : null, parsed);
        }

        private static string BuildRelativePath(string pagePath, Uri baseUri)
        {
            if (pagePath.StartsWith("/"))
                return pagePath; // absolute uri

            var path = new StringBuilder();

            for (var i = 0; i < baseUri.Segments.Length; i++)
            {
                var segment = baseUri.Segments[i];

                // if this is the last segment and it looks like a file, skip it
                if (i == baseUri.Segments.Length - 1 && segment.Contains("."))
                    break;

                path.Append(segment);
            }

            path.Append(pagePath);

            return path.ToString();
        }
    }
}