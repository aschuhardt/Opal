namespace Opal.Document;

public class FormattedEndLine : ILine
{
    public LineType LineType => LineType.FormattedEnd;

    public override string ToString()
    {
        return "```";
    }
}