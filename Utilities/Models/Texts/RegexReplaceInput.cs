using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts;

public class RegexReplaceInput
{
    [Display("Regular expression")]
    public string Regex { get; set; }

    [Display("Replace pattern")]
    public string Replace { get; set; }
}