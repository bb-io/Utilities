using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.XMLFiles;

public record XliffNoteDto(string SegmentId, string Note)
{
    [Display("Segment ID")]
    public string SegmentId { get; set; } = SegmentId;

    [Display("Note")]
    public string Note { get; set; } = Note;
}