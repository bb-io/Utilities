using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files
{
    public class FilesToZipRequest
    {
        public IEnumerable<FileReference> Files { get; set; }
    }
}
