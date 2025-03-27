using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Numbers.Requests;

public class ConvertTextsToNumbersRequest
{
    [Display("Numeric strings")]
    public IEnumerable<string> NumericStrings { get; set; } = default!;
}