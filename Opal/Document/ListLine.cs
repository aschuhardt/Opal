namespace Opal.Document;

public class ListLine : TextualLineBase
{
    public ListLine(string text) : base(text)
    {
    }

    public override LineType LineType => LineType.List;

    public override string ToString()
    {
        return $"* {Text}";
    }
}