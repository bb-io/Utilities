using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.Models.Arrays.Request;

public class ArrayFindMatchingTextValueRequest
{
    public IEnumerable<string> Array { get; set; } = [];

    [Display("Regular expression")]
    public string Regex { get; set; } = string.Empty;

    [Display("Regex flags")]
    [StaticDataSource(typeof(RegexFlagsSourceHandler))]
    public IEnumerable<string>? Flags { get; set; }
}
