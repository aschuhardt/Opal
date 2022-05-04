namespace Opal.Document;

public class HeadingLine : TextualLineBase
{
    public HeadingLine(string text, int level) : base(text)
    {
        Level = level;
    }

    public int Level { get; }

    public override LineType LineType => LineType.Heading;

    public override string ToString()
    {
        return new string('#', Level) + Text;
    }
}