using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.DataSourceHandlers
{
    public class EncodingSourceHandler : IStaticDataSourceHandler
    {
        public Dictionary<string, string> GetData()
        {
            return new Dictionary<string, string>
        {
            { "utf8", "UTF-8 (no BOM)" },
            { "utf8bom", "UTF-8 with BOM" },
            { "utf16le", "UTF-16LE" }
        };
        }
    }
}
