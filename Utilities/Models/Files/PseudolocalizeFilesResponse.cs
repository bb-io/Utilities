using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files;

public class PseudolocalizeFilesResponse
{
    [Display("Files")]
    public List<FileReference> Files { get; set; } = [];
}
