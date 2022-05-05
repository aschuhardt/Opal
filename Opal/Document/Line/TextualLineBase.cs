namespace Opal.Document.Line;

public abstract class TextualLineBase : ILine
{
    protected TextualLineBase(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public abstract LineType LineType { get; }
}