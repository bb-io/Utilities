using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Arrays.Response;

public class ArrayFindMatchingTextValueResponse
{
    [Display("Matching text")]
    public string MatchingText { get; set; } = string.Empty;
}
