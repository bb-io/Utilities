using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers
{
    public class VersionTypeSourceHandler : IStaticDataSourceItemHandler
    {

        public IEnumerable<DataSourceItem> GetData()
        {
            return new List<DataSourceItem>
            {
                new DataSourceItem("major", "Major" ),
                new DataSourceItem("minor", "Minor"),
                new DataSourceItem("patch", "Patch")
            };
        }
    }
}
