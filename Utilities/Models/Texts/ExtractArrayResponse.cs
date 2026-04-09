using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts;

public record ExtractArrayResponse(List<string> Response)
{
    [Display("Extracted values")]
    public List<string> Response { get; set; } = Response;
}
