using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts
{
    public class ExtractArrayResponse
    {
        [Display("Extracted values")]
        public List<string> Response { get; set; }
    }
}
