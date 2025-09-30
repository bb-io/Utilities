using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class AddNoteToXliffRequest
{
    public FileReference File { get; set; } = new();

    [Display("Segment states to add notes into (all except 'final' by default)")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? RawStatesToProcess { get; set; }

    [Display("Segment states to be added as note ('final' by default)")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? RawStatesToNote { get; set; }

    [Display("Previous and following units to include (default 3)", Description = "Specify how many units to include on each side. Only units with at least one segment to note  will be included")]
    public int? NeighbouringUnitsToInclude { get; set; }

    [Display("Add segment state to note")]
    public bool? AddSegmentState { get; set; }

    [Display("Add quality score to note")]
    public bool? AddQualityScore { get; set; }

    [Display("Add context segments to note")]
    public bool? AddContext { get; set; }
}
