using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.Models.Texts
{
    public class RegexInput
    {
        [Display("Regular Expression")]
        public string Regex { get; set; }

        [Display("Replace pattern")]
        public string? Replace { get; set; }

        [Display("Group Number")]
        public string? Group { get; set; }

        [Display("Replace value from")]
        public IEnumerable<string>? From { get; set; }

        [Display("Replace value to")]
        public IEnumerable<string>? To { get; set; }

        [Display("Regex Flags")]
        [StaticDataSource(typeof(RegexFlagsSourceHandler))]
        public IEnumerable<string>? Flags { get; set; }

    }
}
