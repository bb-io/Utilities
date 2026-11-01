using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Po;

public class PoHeaderRequest
{
    [Display("Header name")]
    public string HeaderName { get; set; } = string.Empty;
}
