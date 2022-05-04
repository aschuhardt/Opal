namespace Opal.Document;

public class LinkLine : TextualLineBase
{
    internal LinkLine(string text, Uri uri) : base(text)
    {
        Uri = uri;
    }

    public Uri Uri { get; }

    public override LineType LineType => LineType.Link;

    public override string ToString()
    {
        return $"=> {Uri}" + (!string.IsNullOrWhiteSpace(Text) ? $" {Text}" : string.Empty);
    }
}