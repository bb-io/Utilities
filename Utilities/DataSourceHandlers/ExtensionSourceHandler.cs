using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers
{
    public class ExtensionSourceHandler : IStaticDataSourceItemHandler
    {
        public IEnumerable<DataSourceItem> GetData()
        {
            var extension = new List<string>
            {
                ".txt",
                ".doc",
                ".docx",
                ".html",
                ".json"
            };

            return extension.Select(x=> new DataSourceItem
            {
                Value= x,
                DisplayName= x
            });
        }
    }
}
