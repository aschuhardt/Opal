namespace Opal.Document.Line
{
    public class FormattedLine : TextualLineBase
    {
        public FormattedLine(string text) : base(text)
        {
        }

        public override LineType LineType => LineType.Formatted;

        public override string ToString()
        {
            return Text;
        }
    }
}