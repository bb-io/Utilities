using Apps.Utilities.ErrorWrapper;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;

namespace Apps.Utilities.Utils.DocumentReader.Concrete;

public class RtfReader : IDocumentReader
{
    public async Task<string> Read(Stream file)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return await ErrorWrapperExecute.ExecuteSafelyAsync(async () =>
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var reader = new StreamReader(memoryStream);
            var rtfContent = await reader.ReadToEndAsync();

            var htmlContent = RtfPipe.Rtf.ToHtml(rtfContent);

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var text = doc.DocumentNode.InnerText;
            return Regex.Replace(text, @"\s+", " ").Trim();
        });
    }
}
