using Apps.Utilities.DataSourceHandlers;
using Apps.Utilities.Models.Enums;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
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

    [Display("Null value handling strategy"), StaticDataSource(typeof(NullValueHandlingHandler))]
    public string? NullValueHandlingStrategy { get; set; }

    public NullValueHandlingStrategy GetNullValueHandlingStrategy()
    {
        return NullValueHandlingStrategy switch
        {
            "ignore" => Enums.NullValueHandlingStrategy.Ignore,
            "include" => Enums.NullValueHandlingStrategy.Include,
            "error" => Enums.NullValueHandlingStrategy.Error,
            _ => Enums.NullValueHandlingStrategy.Include
        };
    }
}