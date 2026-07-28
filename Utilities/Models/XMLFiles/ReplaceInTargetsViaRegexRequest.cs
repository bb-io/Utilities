using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class ReplaceInTargetsViaRegexRequest
{
    [Display("File", Description = "XLIFF file or another file type supported by Blackbird Filters.")]
    public FileReference File { get; set; } = new();

    [Display("Regex pattern", Description = "Regular expression applied to visible target text.")]
    public string RegexPattern { get; set; } = string.Empty;

    [Display("Replacement", Description = "Replacement text. Leave empty to remove matching text.")]
    public string? Replacement { get; set; }

    [Display("Target match pattern", Description = "Only process a target when its complete visible text matches this regular expression.")]
    public string? TargetMatchPattern { get; set; }

    [Display("Segment states", Description = "Only process segments in one of these states. Leave empty to process all states.")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? SegmentStates { get; set; }

    [Display("Output file format", Description = "Return the original file format by default, or create the selected XLIFF version.")]
    [StaticDataSource(typeof(XliffOutputFormatDataSourceHandler))]
    public string? OutputFileFormat { get; set; }
}
