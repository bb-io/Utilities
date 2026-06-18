using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using System.ComponentModel;

namespace Apps.Utilities.Models.XMLFiles;

public class CheckXliffCharacterLimitsRequest
{
    [Display("XLIFF file")]
    public FileReference File { get; set; } = new();

    [Display("Segment states to include"), Description("Check only units with at least one segment in any selected state. Leave empty to check all units.")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? SegmentStatesToInclude { get; set; }
}
