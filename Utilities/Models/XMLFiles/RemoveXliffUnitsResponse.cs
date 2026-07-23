using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class RemoveXliffUnitsResponse
{
    [Display("File with filtered segments")]
    public required FileReference File { get; set; }

    [Display("Total segments before")]
    public int TotalSegmentsBefore { get; set; }

    [Display("Total segments after")]
    public int TotalSegmentsAfter { get; set; }

    [Display("Units left")]
    public int UnitsLeft { get; set; }

    [Display("Removed segments by state")]
    public int RemovedSegmentsByState { get; set; }

    [Display("Removed segments with empty target")]
    public int RemovedSegmentsWithEmptyTarget { get; set; }

    [Display("Removed segments under quality threshold")]
    public int RemovedSegmentsUnderQualityThreshold { get; set; }
}
