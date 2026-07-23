using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class RemoveXliffUnitsRequest
{
    [Display("XLIFF file")]
    public FileReference File { get; set; } = new();

    [Display("Segment states to remove", Description = "Remove units where every segment has one of these states. Defaults to Final.")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? SegmentStates { get; set; }

    [Display("Remove units with empty targets", Description = "Remove units where every segment target is empty or contains only whitespace.")]
    public bool? RemoveUnitsWithEmptyTargets { get; set; }

    [Display("Remove units under quality threshold", Description = "Remove units whose quality score is below the supplied limit or the threshold stored in the XLIFF.")]
    public bool? RemoveUnitsUnderQualityThreshold { get; set; }

    [Display("Overwrite quality threshold limit", Description = "Optional score from 0 to 100. When supplied, this limit overrides thresholds stored in the XLIFF and enables quality filtering.")]
    public double? QualityThresholdLimit { get; set; }

    [Display("Strip skeleton", Description = "Remove the XLIFF skeleton content or reference. Defaults to true.")]
    public bool? StripSkeleton { get; set; }
}
