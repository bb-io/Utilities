using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts;

public class RegexReplaceMultipleInput
{
    [Display("Regular expressions")]
    public IEnumerable<string> RegexPatterns { get; set; } = [];

    [Display("Replacement strings")]
    public IEnumerable<string> Replacements { get; set; } = [];
}
