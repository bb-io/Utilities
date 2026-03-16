using Apps.Utilities.ErrorWrapper;
using DocumentFormat.OpenXml.Packaging;

namespace Apps.Utilities.Utils.DocumentReader.Concrete;

public class DocxReader : IDocumentReader
{
    public async Task<string> Read(Stream file)
    {
        return await ErrorWrapperExecute.ExecuteSafelyAsync(async () =>
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var document = WordprocessingDocument.Open(memoryStream, false);
            return document.MainDocumentPart?.Document.Body?.InnerText ?? string.Empty;
        });
    }
}
