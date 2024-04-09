using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts
{
    public class RegexReplaceInput
    {
        [Display("Regular Expression")]
        public string Regex { get; set; }

        [Display("Replace Pattern")]
        public string Replace { get; set; }
    }
}