using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Arrays.Request;

public class ArrayIntersectionRequest
{
    [Display("Array #1")]
    public IEnumerable<string> FirstArray { get; set; }

    [Display("Array #2")]
    public IEnumerable<string> SecondArray { get; set; }
}