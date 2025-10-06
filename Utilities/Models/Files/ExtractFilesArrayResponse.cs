using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files
{
    public class ExtractFilesArrayResponse
    {
        [Display("Fili names")]
        public List<string> Files { get; set; }
    }
}
