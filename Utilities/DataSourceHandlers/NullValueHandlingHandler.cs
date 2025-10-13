
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers;

public class NullValueHandlingHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>
        {
            new("include", "Include (default)"),
            new("ignore", "Ignore"),
            new("error", "Throw error")
        };
    }
}