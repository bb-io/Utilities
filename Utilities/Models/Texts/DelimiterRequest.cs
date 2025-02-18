using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts;

public class DelimiterRequest
{
    [Display("Delimiter", Description = "The delimiter used to split the string. Defaults to ','.")]
    public string Delimiter { get; set; } = string.Empty;
}