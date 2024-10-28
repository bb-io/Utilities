using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Arrays.Request;

public class ArrayFilterRequest
{
    public IEnumerable<string> Array { get; set; }

    [Display("Entries to keep")]
    public IEnumerable<string> Control { get; set; }
}