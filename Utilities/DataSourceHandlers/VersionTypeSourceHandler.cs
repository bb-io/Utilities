using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.DataSourceHandlers
{
    public class VersionTypeSourceHandler : IStaticDataSourceHandler
    {

        public Dictionary<string, string> GetData()
        {
            return new Dictionary<string, string>()
            {
                {"major", "Major" },
                {"minor", "Minor" },
                {"patch", "Patch" }
            };
        }
    }
}
