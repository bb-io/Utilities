using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Texts;

public class RemoveHtmlTagsRequest
{
    [Display("HTML text", Description = "HTML content to convert to plain text.")]
    public string? Html { get; set; }

    [Display("HTML file", Description = "HTML document to convert to plain text.")]
    public FileReference? File { get; set; }
}
