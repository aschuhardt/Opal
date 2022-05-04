namespace Opal.Document;

public class FormattedBeginLine : TextualLineBase
{
    internal FormattedBeginLine(string altText) : base(altText)
    {
    }

    public override LineType LineType => LineType.FormattedBegin;

    public override string ToString()
    {
        return "```" + (!string.IsNullOrWhiteSpace(Text) ? Text : string.Empty);
    }
}