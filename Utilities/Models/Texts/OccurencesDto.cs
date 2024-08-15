using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts
{
    public class OccurencesDto
    {
        [Display("Options", Description = "Will return multiple texts of all these options if they occur in the text.")]
        public IEnumerable<string> Options { get; set; }
    }
}
