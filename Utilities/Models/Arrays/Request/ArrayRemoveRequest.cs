using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Arrays.Request;

public class ArrayRemoveRequest
{
    public IEnumerable<string> Array { get; set; }

    [Display("Entries to remove")]
    public IEnumerable<string> removeEntries { get; set; }
}