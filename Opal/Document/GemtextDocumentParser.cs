using Opal.Document.Line;

namespace Opal.Document;

/// <summary>
/// The default implementation of a Gemtext document parser.
/// </summary>
public class GemtextDocumentParser : IGemtextDocumentParser
{
    private readonly Uri _uri;

    public GemtextDocumentParser(Uri uri)
    {
        _uri = uri;
    }

    public IEnumerable<ILine> ParseDocument(Stream body)
    {
        using var reader = new StreamReader(body);
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
                    linkUri = new Uri(_uri, linkUri);
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