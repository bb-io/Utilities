using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Arrays.Request;
public class ArrayLookupRequest
{
    [Display("Array")]
    public IEnumerable<object> Array { get; set; } = [];

    [Display("Property to match")]
    public string LookupPropertyName { get; set; } = string.Empty;

    [Display("Value to match")]
    public string LookupPropertyValue { get; set; } = string.Empty;

    [Display("Output property")]
    public string ResultPropertyName { get; set; } = string.Empty;
}
