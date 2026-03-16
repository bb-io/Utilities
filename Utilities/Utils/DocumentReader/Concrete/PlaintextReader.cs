using System.Text;

namespace Apps.Utilities.Utils.DocumentReader.Concrete;

public class PlaintextReader : IDocumentReader
{
    public async Task<string> Read(Stream file)
    {
        var stringBuilder = new StringBuilder();
        using (var reader = new StreamReader(file))
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                stringBuilder.AppendLine(line);
            }
        }

        var document = stringBuilder.ToString();
        return document;
    }
}
