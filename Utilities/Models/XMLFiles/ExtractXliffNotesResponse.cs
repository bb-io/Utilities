using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.XMLFiles;

public record ExtractXliffNotesResponse(List<string> SegmentIds, List<string> SegmentNotes)
{
    [Display("Segment IDs")]
    public List<string> SegmentIds { get; set; } = SegmentIds;

    [Display("Segment notes", Description = "Each note corresponds to the segment ID at the same index")]
    public List<string> SegmentNotes { get; set; } = SegmentNotes;
}