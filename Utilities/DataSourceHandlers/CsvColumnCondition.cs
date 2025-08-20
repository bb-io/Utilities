using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers;
public class CsvColumnCondition : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>()
        {
            new DataSourceItem("is_empty", "Is empty"),
            new DataSourceItem("is_full", "Is full"),
            new DataSourceItem("value_equals","Value equals"),
            new DataSourceItem("value_contains","Value contains")
        };
    }
}
