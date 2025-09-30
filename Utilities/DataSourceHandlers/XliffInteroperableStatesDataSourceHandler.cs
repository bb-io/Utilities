using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Filters.Enums;

namespace Apps.Utilities.DataSourceHandlers;

public class XliffInteroperableStatesDataSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()=>
    [
        new(SegmentStateHelper.Serialize(SegmentState.Initial), "Initial or empty"),
        new(SegmentStateHelper.Serialize(SegmentState.Translated), "Translated"),
        new(SegmentStateHelper.Serialize(SegmentState.Reviewed), "Reviewed"),
        new(SegmentStateHelper.Serialize(SegmentState.Final), "Final"),
    ];
}
