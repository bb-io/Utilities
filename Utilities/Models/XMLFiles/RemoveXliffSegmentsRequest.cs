using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class RemoveXliffSegmentsRequest
{
    [Display("XLIFF file")]
    public FileReference File { get; set; } = new();

    [Display("Segment states to keep", Description = "Keep segments with one of these states. Keeps all states except Final by default.")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? SegmentStatesToKeep { get; set; }

    [Display("Keep segments with empty targets", Description = "Keep segments whose target is empty or contains only whitespace.")]
    public bool? KeepSegmentsWithEmptyTargets { get; set; }

    [Display("Keep segments under quality threshold", Description = "Keep segments whose unit quality score is below the supplied limit or the threshold stored in the XLIFF.")]
    public bool? KeepSegmentsUnderQualityThreshold { get; set; }

    [Display("Overwrite quality threshold limit", Description = "Optional score from 0 to 100. When supplied, this limit overrides thresholds stored in the XLIFF and enables keeping segments under the threshold.")]
    public double? QualityThresholdLimit { get; set; }

    [Display("Strip skeleton", Description = "Remove the XLIFF skeleton content or reference. Defaults to true.")]
    public bool? StripSkeleton { get; set; }
}
