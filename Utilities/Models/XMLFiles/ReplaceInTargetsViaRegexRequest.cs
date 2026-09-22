using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class ReplaceInTargetsViaRegexRequest
{
    [Display("File", Description = "XLIFF file or another file type supported by Blackbird Filters.")]
    public FileReference File { get; set; } = new();

    [Display("Regex pattern", Description = "Regular expression applied to visible text in the selected target, source, or both.")]
    public string RegexPattern { get; set; } = string.Empty;

    [Display("Replace in", Description = "Choose target only, source only, or both target and source. Defaults to target only.")]
    [StaticDataSource(typeof(XliffReplacementScopeDataSourceHandler))]
    public string? ReplaceIn { get; set; }

    [Display("Replacement", Description = "Replacement text. Leave empty to remove matching text.")]
    public string? Replacement { get; set; }

    [Display("Target match pattern", Description = "Only process segments whose complete visible target text matches this regular expression, including when replacing source text.")]
    public string? TargetMatchPattern { get; set; }

    [Display("Segment states", Description = "Only process segments in one of these states. Leave empty to process all states.")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? SegmentStates { get; set; }

    [Display("Output file format", Description = "Output the original file format by default, or create the selected XLIFF version.")]
    [StaticDataSource(typeof(XliffOutputFormatDataSourceHandler))]
    public string? OutputFileFormat { get; set; }
}
