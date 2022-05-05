using Opal.Document.Line;

namespace Opal.Document;

/// <summary>
///     The default implementation of a Gemtext document parser.
/// </summary>
public class GemtextDocumentParser : IGemtextDocumentParser
{
    private const string LinePrefixLink = "=> ";
    private const string LinePrefixFormattedToggle = "```";
    private const string LinePrefixHeading1 = "# ";
    private const string LinePrefixHeading2 = "## ";
    private const string LinePrefixHeading3 = "### ";
    private const string LinePrefixList = "* ";
    private const string LinePrefixQuote = "> ";
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

        if (formatted && firstChar != '`')
            return new FormattedLine(line);

        return line switch
        {
            _ when line.StartsWith(LinePrefixLink) => ParseLinkLinePrefix(line),
            _ when line.StartsWith(LinePrefixFormattedToggle) && !formatted => ParsePrefixLineAndToggle(line,
                ref formatted, text => new FormattedBeginLine(text)),
            _ when line.StartsWith(LinePrefixFormattedToggle) && formatted => ParsePrefixLineAndToggle(line,
                ref formatted, _ => new FormattedEndLine()),
            _ when line.StartsWith(LinePrefixHeading1) => ParsePrefixLine(line, text => new HeadingLine(text, 1)),
            _ when line.StartsWith(LinePrefixHeading2) => ParsePrefixLine(line, text => new HeadingLine(text, 2)),
            _ when line.StartsWith(LinePrefixHeading3) => ParsePrefixLine(line, text => new HeadingLine(text, 3)),
            _ when line.StartsWith(LinePrefixQuote) => ParsePrefixLine(line, text => new QuoteLine(text)),
            _ when line.StartsWith(LinePrefixList) => ParsePrefixLine(line, text => new ListLine(text)),
            _ when string.IsNullOrWhiteSpace(line) => new EmptyLine(),
            _ => new TextLine(line)
        };
    }

    private static ILine ParsePrefixLineAndToggle<T>(string line, ref bool flag, Func<string, T> create) where T : ILine
    {
        flag = !flag;
        return line.Contains(' ')
            ? create(line[line.IndexOf(' ')..].Trim())
            : create(null);
    }

    private static ILine ParsePrefixLine<T>(string line, Func<string, T> create) where T : ILine
    {
        var text = line[line.IndexOf(' ')..];
        return string.IsNullOrWhiteSpace(text)
            ? new TextLine(line)
            : create(text.Trim());
    }

    private bool TryBuildNormalizedUri(string uri, out Uri parsed)
    {
        if (!uri.StartsWith('/')) 
            return Uri.TryCreate(uri, UriKind.Absolute, out parsed);

        // build an absolute URI from a relative one
        var path = uri;
        var query = string.Empty;
        if (uri.Contains('?'))
        {
            path = uri[..uri.IndexOf('?')];
            query = uri[(uri.IndexOf('?') + 1)..].TrimEnd();
        }

        parsed = new UriBuilder
        {
            Host = _uri.Host, Scheme = _uri.Scheme, Port = _uri.IsDefaultPort ? -1 : _uri.Port, Path = path,
            Query = query
        }.Uri;

        return true;
    }

    private ILine ParseLinkLinePrefix(string line)
    {
        var trimmedAfterPrefix = line[line.IndexOf(' ')..]?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedAfterPrefix))
            return new TextLine(line);

        if (!trimmedAfterPrefix.Contains(' '))
            return !TryBuildNormalizedUri(trimmedAfterPrefix, out var parsed)
                ? new TextLine(line)
                : new LinkLine(null, parsed);
        else
            return !TryBuildNormalizedUri(trimmedAfterPrefix[..trimmedAfterPrefix.IndexOf(' ')], out var parsed)
                ? new TextLine(line)
                : new LinkLine(trimmedAfterPrefix[trimmedAfterPrefix.IndexOf(' ')..].Trim(), parsed);
    }
}