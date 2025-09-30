using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class AddNoteToXliffRequest
{
    public FileReference File { get; set; } = new();

    [Display("Segment states to add notes to (all except 'final' by default)")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? RawStatesToProcess { get; set; }

    [Display("Segment states to include as notes ('final' by default)")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? RawStatesToNote { get; set; }

    [Display("Include neighboring units in note")]
    public bool? IncludeNeigboringUnits { get; set; }

    [Display("Number of neighboring units to include on each side (default 3)")]
    public int? NeighbouringUnitsToInclude { get; set; }

    [Display("Include segment state in note")]
    public bool? IncludeSegmentState { get; set; }

    [Display("Include quality score in note")]
    public bool? IncludeQualityScore { get; set; }
}
