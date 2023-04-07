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
    [TestCase("=>\t/something", typeof(LinkLine))]
    [TestCase("=>\t\tgemini://localhost/something", typeof(LinkLine))]
    [TestCase("=>\t /something else", typeof(LinkLine))]
    [TestCase("=>gemini://localhost/something else", typeof(LinkLine))]
    [TestCase("=>/something", typeof(LinkLine))]
    [TestCase("=>gemini://localhost/something", typeof(LinkLine))]
    [TestCase("=>/something else", typeof(LinkLine))]
    [TestCase("=>gemini://localhost/something else", typeof(LinkLine))]
    [TestCase("=>     /something", typeof(LinkLine))]
    [TestCase("=>   gemini://localhost/something", typeof(LinkLine))]
    [TestCase("=> http://localhost/something", typeof(LinkLine))]
    [TestCase("=>    http://localhost/something", typeof(LinkLine))]
    [TestCase("=>http://localhost/something else", typeof(LinkLine))]
    [TestCase("=>http://localhost/something", typeof(LinkLine))]
    [TestCase("=>    http://localhost/something else", typeof(LinkLine))]
    [TestCase("* something", typeof(ListLine))]
    [TestCase("* 연주가", typeof(ListLine))]
    [TestCase("*    something", typeof(ListLine))]
    [TestCase("*\tsomething", typeof(ListLine))]
    [TestCase("*\t연주가", typeof(ListLine))]
    [TestCase("*\t \t  something", typeof(ListLine))]
    [TestCase("*something", typeof(ListLine))]
    [TestCase("*연주가", typeof(ListLine))]
    [TestCase("", typeof(EmptyLine))]
    [TestCase("   ", typeof(EmptyLine))]
    [TestCase("\t\t", typeof(EmptyLine))]
    [TestCase("\t", typeof(EmptyLine))]
    [TestCase("*", typeof(TextLine))]
    [TestCase("* ", typeof(TextLine))]
    [TestCase("=", typeof(TextLine))]
    [TestCase("=> ", typeof(TextLine))]
    [TestCase("> ", typeof(TextLine))]
    [TestCase("# ", typeof(TextLine))]
    [TestCase("## ", typeof(TextLine))]
    [TestCase("### ", typeof(TextLine))]
    [TestCase("something", typeof(TextLine))]
    [TestCase("연주가", typeof(TextLine))]
    [TestCase("    something", typeof(TextLine))]
    [TestCase("    연주가", typeof(TextLine))]
    [TestCase("> something", typeof(QuoteLine))]
    [TestCase("> 연주가", typeof(QuoteLine))]
    [TestCase(">       something", typeof(QuoteLine))]
    [TestCase(">\tsomething", typeof(QuoteLine))]
    [TestCase(">\t연주가", typeof(QuoteLine))]
    [TestCase(">\t\t\tsomething", typeof(QuoteLine))]
    [TestCase(">something", typeof(QuoteLine))]
    [TestCase(">연주가", typeof(QuoteLine))]
    [TestCase("# something", typeof(HeadingLine))]
    [TestCase("# 연주가", typeof(HeadingLine))]
    [TestCase("#\tsomething", typeof(HeadingLine))]
    [TestCase("#\t \t연주가", typeof(HeadingLine))]
    [TestCase("#\t\t연주가", typeof(HeadingLine))]
    [TestCase("#something", typeof(HeadingLine))]
    [TestCase("#연주가", typeof(HeadingLine))]
    [TestCase("#       something", typeof(HeadingLine))]
    [TestCase("## something", typeof(HeadingLine))]
    [TestCase("## 연주가", typeof(HeadingLine))]
    [TestCase("##something", typeof(HeadingLine))]
    [TestCase("##연주가", typeof(HeadingLine))]
    [TestCase("##       something", typeof(HeadingLine))]
    [TestCase("### something", typeof(HeadingLine))]
    [TestCase("### 연주가", typeof(HeadingLine))]
    [TestCase("###something", typeof(HeadingLine))]
    [TestCase("###연주가", typeof(HeadingLine))]
    [TestCase("###       something", typeof(HeadingLine))]
    [TestCase("``` something", typeof(FormattedBeginLine))]
    [TestCase("``` 연주가", typeof(FormattedBeginLine))]
    [TestCase("```\tsomething", typeof(FormattedBeginLine))]
    [TestCase("```\t\t연주가", typeof(FormattedBeginLine))]
    [TestCase("```", typeof(FormattedBeginLine))]
    public void DocumentParser_ResolvesCorrectLineType(string line, Type expectedType)
    {
        var withLineEnding = new StringBuilder(line, line.Length + 1).AppendLine();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(withLineEnding.ToString()));
        Assert.IsAssignableFrom(expectedType, _parser.ParseDocument(stream).Single());
    }

    [TestCase("=> /something/else", null, "/something/else", null)]
    [TestCase("=> /something/else hello world", "hello world", "/something/else", null)]
    [TestCase("=> /something/else?someInput hello world", "hello world", "/something/else", "?someInput")]
    [TestCase("=> /something/else?someInput hello 연주가", "hello 연주가", "/something/else", "?someInput")]
    [TestCase("=> /something/else?연주가", null, "/something/else", "?%EC%97%B0%EC%A3%BC%EA%B0%80")]
    [TestCase("=> /something/else?연주가 hello", "hello", "/something/else", "?%EC%97%B0%EC%A3%BC%EA%B0%80")]
    [TestCase("=> users/ Users with Gemini content", "Users with Gemini content", "/users/", null)]
    [TestCase("=> /something/else", null, "/something/else", null)]
    [TestCase("=> /something/else\thello world", "hello world", "/something/else", null)]
    [TestCase("=> /something/else?someInput\thello world", "hello world", "/something/else", "?someInput")]
    [TestCase("=> /something/else?someInput\thello 연주가", "hello 연주가", "/something/else", "?someInput")]
    [TestCase("=> /something/else?연주가", null, "/something/else", "?%EC%97%B0%EC%A3%BC%EA%B0%80")]
    [TestCase("=> /something/else?연주가\thello", "hello", "/something/else", "?%EC%97%B0%EC%A3%BC%EA%B0%80")]
    [TestCase("=> users/\tUsers with Gemini content", "Users with Gemini content", "/users/", null)]
    [TestCase("=>\t/something/else", null, "/something/else", null)]
    [TestCase("=>\t/something/else hello world", "hello world", "/something/else", null)]
    [TestCase("=>\t/something/else?someInput hello world", "hello world", "/something/else", "?someInput")]
    [TestCase("=>\t/something/else?someInput hello 연주가", "hello 연주가", "/something/else", "?someInput")]
    [TestCase("=>\t/something/else?연주가", null, "/something/else", "?%EC%97%B0%EC%A3%BC%EA%B0%80")]
    [TestCase("=>\t/something/else?연주가 hello", "hello", "/something/else", "?%EC%97%B0%EC%A3%BC%EA%B0%80")]
    [TestCase("=>\tusers/ Users with Gemini content", "Users with Gemini content", "/users/", null)]
    [TestCase("=>\t/something/else", null, "/something/else", null)]
    [TestCase("=>\t/something/else\thello world", "hello world", "/something/else", null)]
    [TestCase("=>\t/something/else?someInput\thello world", "hello world", "/something/else", "?someInput")]
    [TestCase("=>\t/something/else?someInput\thello 연주가", "hello 연주가", "/something/else", "?someInput")]
    [TestCase("=>\t/something/else?연주가", null, "/something/else", "?%EC%97%B0%EC%A3%BC%EA%B0%80")]
    [TestCase("=>\t/something/else?연주가\thello", "hello", "/something/else", "?%EC%97%B0%EC%A3%BC%EA%B0%80")]
    [TestCase("=>\tusers/\tUsers with Gemini content", "Users with Gemini content", "/users/", null)]
    [TestCase("=>\t\t\t  \t\t/something/else", null, "/something/else", null)]
    [TestCase("=>  \t \t/something/else\thello world", "hello world", "/something/else", null)]

    public void DocumentParser_ParsesRelativeLinkLinesCorrectly(string line, string expectedText,
        string? expectedPath, string? expectedQuery)
    {
        var withLineEnding = new StringBuilder(line).AppendLine();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(withLineEnding.ToString()));
        if (_parser.ParseDocument(stream).Single() is not LinkLine linkLine) 
            throw new ArgumentException();

        Assert.AreEqual(expectedText, linkLine.Text);

        var uri = linkLine.Uri;
        Assert.AreEqual(_localhost.Scheme, uri.Scheme);
        Assert.AreEqual(_localhost.Host, uri.Host);
        Assert.AreEqual(_localhost.Port, uri.Port);
        Assert.AreEqual(_localhost.IsDefaultPort, uri.IsDefaultPort);
        Assert.AreEqual(expectedPath ?? string.Empty, uri.AbsolutePath);
        Assert.AreEqual(expectedQuery ?? string.Empty, uri.Query);
    }

    [TestCase("=> //example.com", "example.com", "/")]
    [TestCase("=> //example.com/file.txt", "example.com", "/file.txt")]
    public void DocumentParser_ParsesProtocolRelativeUrlsCorrectly(string line, string expectedHost, string expectedPath)
    {
        var withLineEnding = new StringBuilder(line).AppendLine();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(withLineEnding.ToString()));
        if (_parser.ParseDocument(stream).Single() is not LinkLine linkLine) 
            throw new ArgumentException();

        var uri = linkLine.Uri;
        Assert.AreEqual(expectedHost, uri.Host);
        Assert.AreEqual(expectedPath, uri.PathAndQuery);
    }
}