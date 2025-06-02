using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers
{
    public class TimeZoneSourceHandler : IStaticDataSourceItemHandler
    {
        public IEnumerable<DataSourceItem> GetData()
        {
            return TimeZoneInfo.GetSystemTimeZones()
                .Select(tz => new DataSourceItem(tz.Id, tz.DisplayName));
        }
    }
}
