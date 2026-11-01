using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Po;

public class SetPoHeaderRequest : PoHeaderRequest
{
    [Display("Header value")]
    public string HeaderValue { get; set; } = string.Empty;
}
