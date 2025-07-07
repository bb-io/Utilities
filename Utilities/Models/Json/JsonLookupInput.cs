using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Json;
public class JsonLookupInput
{
    [Display("JSON file")]
    public FileReference File { get; set; } = new();

    [Display("Path to array property")]
    public string LookupArrayPropertyPath { get; set; } = string.Empty;

    [Display("Path to property to match")]
    public string LookupPropertyPath { get; set; } = string.Empty;

    [Display("Value to match")]
    public string LookupPropertyValue { get; set; } = string.Empty;

    [Display("Output property path")]
    public string ResultPropertyPath { get; set; } = string.Empty;
}
