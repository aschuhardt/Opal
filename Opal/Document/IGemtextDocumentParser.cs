using Opal.Document.Line;

namespace Opal.Document;

public interface IGemtextDocumentParser
{
    IEnumerable<ILine> ParseDocument(Stream body);
}