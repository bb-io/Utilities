using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts
{
    public class TextsDto
    {
        [Display("Text")]
        public IEnumerable<string> Texts { get; set; }
    }
}
