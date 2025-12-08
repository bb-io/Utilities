using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts;

public class CompareTextsRequest
{
    [Display("Texts to compare")]
    public List<string> Texts { get; set; }
}
