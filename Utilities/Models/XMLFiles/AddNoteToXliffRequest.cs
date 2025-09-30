using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class AddNoteToXliffRequest
{
    public FileReference File { get; set; } = new();

    [Display("Include segment state in note")]
    public bool? IncludeSegmentState { get; set; }

    [Display("Include quality score in note")]
    public bool? IncludeQualityScore { get; set; }

    [Display("Include surrounding units in note")]
    public bool? IncludeSurroundingUnits { get; set; }

    [Display("Number of surrounding units to include on each side (default 3)")]
    public int? SurroundingUnitsToInclude { get; set; }

    [Display("Segment states to add notes to (all except 'final' by default)")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? RawStatesToProcess { get; set; }
}
