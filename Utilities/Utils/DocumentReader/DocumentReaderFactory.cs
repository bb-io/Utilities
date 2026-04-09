using Apps.Utilities.Utils.DocumentReader.Concrete;

namespace Apps.Utilities.Utils.DocumentReader;

public static class DocumentReaderFactory
{
    public static IDocumentReader GetReader(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".pdf" => new PdfReader(),
            ".docx" or ".doc" => new DocxReader(),
            ".html" or ".htm" => new HtmlReader(),
            ".xlsx" => new XlsxReader(),
            ".pptx" => new PptxReader(),
            ".idml" => new IdmlReader(),
            ".rtf" => new RtfReader(),
            _ => new PlaintextReader()
        };
    }
}
