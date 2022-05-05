using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Opal.Document;
using Opal.Document.Line;

namespace Opal.Test;

[TestFixture]
internal class DocumentTests
{
    private readonly Uri _localhost;
    private readonly GemtextDocumentParser _parser;

    public DocumentTests()
    {
        _localhost = new Uri("gemini://localhost");
        _parser = new GemtextDocumentParser(_localhost);
    }

    [TestCase("=> /something", typeof(LinkLine))]
    [TestCase("=> gemini://localhost/something", typeof(LinkLine))]
    [TestCase("=> /something else", typeof(LinkLine))]
    [TestCase("=> gemini://localhost/something else", typeof(LinkLine))]
    [TestCase("=>     /something", typeof(LinkLine))]
    [TestCase("=>   gemini://localhost/something", typeof(LinkLine))]
    [TestCase("=> http://localhost/something", typeof(LinkLine))]
    [TestCase("=>    http://localhost/something", typeof(LinkLine))]
    [TestCase("=> http://localhost/something else", typeof(LinkLine))]
    [TestCase("=>    http://localhost/something else", typeof(LinkLine))]
    [TestCase("* something", typeof(ListLine))]
    [TestCase("*    something", typeof(ListLine))]
    [TestCase("", typeof(EmptyLine))]
    [TestCase("   ", typeof(EmptyLine))]
    [TestCase("*", typeof(TextLine))]
    [TestCase("* ", typeof(TextLine))]
    [TestCase("=", typeof(TextLine))]
    [TestCase("=> ", typeof(TextLine))]
    [TestCase("> ", typeof(TextLine))]
    [TestCase("# ", typeof(TextLine))]
    [TestCase("## ", typeof(TextLine))]
    [TestCase("### ", typeof(TextLine))]
    [TestCase("something", typeof(TextLine))]
    [TestCase("    something", typeof(TextLine))]
    [TestCase("> something", typeof(QuoteLine))]
    [TestCase(">       something", typeof(QuoteLine))]
    [TestCase("# something", typeof(HeadingLine))]
    [TestCase("#       something", typeof(HeadingLine))]
    [TestCase("## something", typeof(HeadingLine))]
    [TestCase("##       something", typeof(HeadingLine))]
    [TestCase("### something", typeof(HeadingLine))]
    [TestCase("###       something", typeof(HeadingLine))]
    [TestCase("``` something", typeof(FormattedBeginLine))]
    [TestCase("```", typeof(FormattedBeginLine))]
    public void DocumentParserResolvesCorrectLineType(string line, Type expectedType)
    {
        var withLineEnding = new StringBuilder(line, line.Length + 1).AppendLine();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(withLineEnding.ToString()));
        Assert.IsAssignableFrom(expectedType, _parser.ParseDocument(stream).Single());
    }
}