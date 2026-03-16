using UglyToad.PdfPig;
using Apps.Utilities.ErrorWrapper;

namespace Apps.Utilities.Utils.DocumentReader.Concrete;

public class PdfReader : IDocumentReader
{
    public async Task<string> Read(Stream file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return await ErrorWrapperExecute.ExecuteSafelyAsync(async () =>
        {
            using var document = PdfDocument.Open(memoryStream);
            return string.Join(" ", document.GetPages().Select(p => p.Text));
        });
    }
}
