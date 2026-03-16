using System.Text;
using Apps.Utilities.ErrorWrapper;
using DocumentFormat.OpenXml.Packaging;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Utilities.Utils.DocumentReader.Concrete;

public class PptxReader : IDocumentReader
{
    public async Task<string> Read(Stream file)
    {
        return await ErrorWrapperExecute.ExecuteSafelyAsync(async () =>
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var document = PresentationDocument.Open(memoryStream, false);
            var presentationPart = document.PresentationPart
                ?? throw new PluginApplicationException("Invalid PPTX file: presentation part is missing.");

            var stringBuilder = new StringBuilder();

            foreach (var slidePart in presentationPart.SlideParts)
            {
                var texts = slidePart.Slide
                    .Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                    .Select(x => x.Text)
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                foreach (var text in texts)
                {
                    stringBuilder.Append(text);
                    stringBuilder.Append(' ');
                }
            }

            return stringBuilder.ToString();
        });
    }
}
