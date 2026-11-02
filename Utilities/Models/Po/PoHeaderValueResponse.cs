using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Po;

public class PoHeaderValueResponse
{
    [Display("Header value")]
    public string HeaderValue { get; set; } = string.Empty;
}
