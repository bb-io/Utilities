using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Shared;

public class SanitizeRequest
{
    [Display("Characters to remove")]
    public IEnumerable<string> FilterCharacters { get; set; }
}