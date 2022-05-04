namespace Opal.Document;

public class EmptyLine : ILine
{
    public LineType LineType => LineType.Empty;

    public override string ToString()
    {
        return string.Empty;
    }
}