

using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Arrays.Request;

public class ArrayAddCreateRequest
{
    [Display("Original Array")]
    public IEnumerable<string>? OriginalArray { get; set; }

    [Display("Array to Add")]
    public IEnumerable<string>? ArrayToBeAdded { get; set; }

}