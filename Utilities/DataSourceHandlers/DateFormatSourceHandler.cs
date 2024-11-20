using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers;

public class DateFormatSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>
        {
        new DataSourceItem  ("d", "Short date"),
        new DataSourceItem  ("D", "Long date"),
        new DataSourceItem  ("f", "Full date/time (short time)"),
        new DataSourceItem  ("F", "Full date/time (long time)"),
        new DataSourceItem  ("g", "General date/time (short time)"),
        new DataSourceItem  ("G", "General date/time (long time)"),
        new DataSourceItem  ("M", "Month/day"),
        new DataSourceItem  ("R", "RFC1123"),
        new DataSourceItem  ("t", "Short time"),
        new DataSourceItem  ("T", "Long time"),
        new DataSourceItem  ("U", "Universal full date/time"),
        new DataSourceItem  ("Y", "Year month")
        };
    }
}