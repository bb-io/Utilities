using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.Models.Files;

public class XliffWordCountOptions
{
    [Display("Filter segments by status", Description = "Count only XLIFF source segments in one of the selected statuses. Leave empty to count all XLIFF segments. This input is ignored for non-XLIFF files.")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? SegmentStates { get; set; }
}
