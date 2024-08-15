using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.DataSourceHandlers
{
    public class ExtensionSourceHandler : IStaticDataSourceHandler
    {
        public Dictionary<string, string> GetData()
        {
            var extension = new List<string>
            {
                ".txt",
                ".doc",
                ".docx"
            };

            return extension.ToDictionary(ext => ext, ext => ext);
        }
    }
}
