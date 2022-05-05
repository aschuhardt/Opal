namespace Opal.Document.Line;

public class TextLine : TextualLineBase
{
    public TextLine(string text) : base(text)
    {
    }

    public override LineType LineType => LineType.Text;

    public override string ToString()
    {
        return Text;
    }
}