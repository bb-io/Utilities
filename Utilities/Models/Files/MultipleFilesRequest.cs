using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files;

public class MultipleFilesRequest
{
    public IEnumerable<FileReference> Files { get; set; }
}
