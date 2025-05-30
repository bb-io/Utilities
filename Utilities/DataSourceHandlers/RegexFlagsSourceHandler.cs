using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.DataSourceHandlers
{
    public class RegexFlagsSourceHandler : IStaticDataSourceHandler
    {
        public Dictionary<string, string> GetData()
        {
            return new Dictionary<string, string>
            {
                { "insensitive", "Case insensitive match" },
                { "multiline", "^ and $ match start/end of line" },
                { "singleline", "Dot matches newline" },
                { "right to left", "Perform matching from right to left" },
                { "non-capturing", "Groups are no longer implicitly capturing" },
                { "no backtracking", "Disable backtracking when matching" },
                { "extended", "Ignore whitespace" }
            };
        }
    }
}
