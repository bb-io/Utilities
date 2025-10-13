using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Json;

public class ChangeJsonPropertyInput
{
    [Display("Property path")]
    public string PropertyPath { get; set; } = default!;

    [Display("New JSON value")]
    public string NewValue { get; set; } = default!;

    [Display("JSON file")]
    public FileReference File { get; set; } = default!;
}