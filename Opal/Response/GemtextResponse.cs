using Opal.Document;

namespace Opal.Response;

public class GemtextResponse : SuccessfulResponse
{
    internal const string GemtextMimeType = "text/gemini;";

    internal GemtextResponse(Uri uri, Stream body, IEnumerable<string> languages) : base(body, uri,
        GemtextMimeType, true)
    {
        Languages = languages;
    }

    public IEnumerable<string> Languages { get; }

    public IEnumerable<ILine> AsDocument()
    {
        using var reader = new StreamReader(Body);
        var formatted = false;
        while (!reader.EndOfStream)
            yield return CreateDocumentLine(reader.ReadLine()?.TrimStart(), ref formatted);
    }

    private ILine CreateDocumentLine(string line, ref bool formatted)
    {
        if (string.IsNullOrWhiteSpace(line))
            return new EmptyLine();

        var firstChar = line[0];
        switch (firstChar)
        {
            case '=' when line[1] == '>':
            {
                var trimmed = line[2..].TrimStart();
                var linkUri = new Uri(trimmed[..trimmed.IndexOf(' ')], UriKind.RelativeOrAbsolute);
                if (!linkUri.IsAbsoluteUri)
                    linkUri = new Uri(Uri, linkUri);
                return new LinkLine(
                    trimmed[(trimmed.IndexOf(' ') + 1)..].TrimStart(), linkUri);
            }
            case '#':
                return new HeadingLine(line[line.IndexOf(' ')..].TrimStart(), line.IndexOf(' '));
            case '>':
            case '*':
                return new ListLine(line[line.IndexOf(' ')..].TrimStart());
            case '`':
            {
                formatted = !formatted;
                return !formatted
                    ? new FormattedEndLine()
                    : new FormattedBeginLine(line[line.IndexOf(' ')..].TrimStart());
            }
            default:
                return new TextLine(line);
        }
    }
}