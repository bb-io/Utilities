using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Files;

public class ExtractTextFromDocumentResponse
{
    [Display("Extracted text")]
    public string ExtractedText { get; set; } = default!;
}