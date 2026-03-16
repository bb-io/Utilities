using HtmlAgilityPack;
using System.Net;
using System.Text;

namespace Apps.Utilities.Utils.DocumentReader.Concrete;

public class HtmlReader : IDocumentReader
{
    public async Task<string> Read(Stream file)
    {
        if (file.CanSeek)
            file.Position = 0;

        using var reader = new StreamReader(file, Encoding.UTF8, leaveOpen: true);
        var html = await reader.ReadToEndAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var body = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;

        var junkNodes = body.SelectNodes(".//script|.//style");
        if (junkNodes is not null)
        {
            foreach (var node in junkNodes)
                node.Remove();
        }

        var sb = new StringBuilder();

        foreach (var node in body.DescendantsAndSelf())
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                var text = WebUtility.HtmlDecode(node.InnerText);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.Append(text);
                    sb.Append(' ');
                }
            }
        }

        return sb.ToString();
    }
}
