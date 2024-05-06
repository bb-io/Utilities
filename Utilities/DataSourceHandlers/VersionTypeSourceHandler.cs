using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
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
