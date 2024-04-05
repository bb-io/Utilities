using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using Apps.Utilities.Models.Scraping;
using HtmlAgilityPack;

namespace Apps.Utilities.Actions
{
    [ActionList]
    public class Scraping : BaseInvocable
    {
        public Scraping(InvocationContext context) : base(context) { }

        [Action("Extract web page content", Description = "Get raw and unformatted content from a URL as text")]
        public async Task<ContentDto> ExtractWebContent([ActionParameter][Display("URL")] string url, [ActionParameter][Display("XPath")] string? xpath)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Edg/122.0.0.0");

            using (var response = await client.GetAsync(url))
            {
                using (var content = response.Content)
                {
                    var result = await content.ReadAsStringAsync();
                    var document = new HtmlDocument();
                    document.LoadHtml(result);

                    var nodes = document.DocumentNode.SelectNodes(xpath ?? "//*[not(self::style) and not(self::script) and not(self::noscript)]/text()[normalize-space(.) != '']");

                    return new ContentDto
                    {
                        Content = nodes == null ? "" : string.Join('\n', nodes.Select(x => HtmlEntity.DeEntitize(x.InnerText.Trim())))
                    };
                }
            }
        }

    }
}
