namespace Opal.Document.Line;

public class QuoteLine : TextualLineBase
{
    public QuoteLine(string text) : base(text)
    {
    }

    public override LineType LineType => LineType.Quote;

    public override string ToString()
    {
        return $"> {Text}";
    }
}