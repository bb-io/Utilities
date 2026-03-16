using System.Text;
using System.Xml.Linq;
using System.IO.Compression;
using Apps.Utilities.ErrorWrapper;

namespace Apps.Utilities.Utils.DocumentReader.Concrete;

public class IdmlReader : IDocumentReader
{
    public async Task<string> Read(Stream file)
    {
        return await ErrorWrapperExecute.ExecuteSafelyAsync(async () =>
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, leaveOpen: false);
            var stringBuilder = new StringBuilder();

            var storyEntries = archive.Entries
                .Where(e => e.FullName.StartsWith("Stories/", StringComparison.OrdinalIgnoreCase)
                         && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

            foreach (var entry in storyEntries)
            {
                using var entryStream = entry.Open();
                var document = await XDocument.LoadAsync(entryStream, LoadOptions.None, CancellationToken.None);

                var contentNodes = document
                    .Descendants()
                    .Where(x => x.Name.LocalName == "Content");

                foreach (var node in contentNodes)
                {
                    var text = node.Value;

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        stringBuilder.Append(text);
                        stringBuilder.Append(' ');
                    }
                }
            }

            return stringBuilder.ToString();
        });
    }
}
