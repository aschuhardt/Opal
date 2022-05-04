using System.Text;
using System.Text.RegularExpressions;

namespace Opal.Header;

internal class GeminiHeader
{
    private const int HeaderPartStatus = 1;
    private const int HeaderPartMeta = 2;

    private static readonly Regex HeaderPattern = new("([1-6][0-9]?)(?: (.*))?\r\n",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex LanguagePattern = new("lang=([a-zA-Z,-])+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private GeminiHeader(int status, string meta)
    {
        StatusCode = status;
        Meta = meta;
    }

    public int StatusCode { get; }

    public string Meta { get; }

    public IEnumerable<string> Languages => TryGetLanguages();

    // not computed by default, since only one type of response will use it
    private IEnumerable<string> TryGetLanguages()
    {
        var match = LanguagePattern.Match(Meta);
        if (!match.Success)
            yield break;

        foreach (var language in match.Groups[1].Value.Split(','))
            yield return language;
    }

    public static GeminiHeader Parse(byte[] buffer, out int end)
    {
        end = default;

        var match = HeaderPattern.Match(Encoding.UTF8.GetString(buffer));
        if (!match.Success || !int.TryParse(match.Groups[HeaderPartStatus].ValueSpan, out var statusCode))
            return null;

        end = match.Length;

        return new GeminiHeader(statusCode, match.Groups[HeaderPartMeta].Value);
    }
}