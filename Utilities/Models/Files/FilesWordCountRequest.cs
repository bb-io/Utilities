using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files;

public class FilesWordCountRequest
{
    public IEnumerable<FileReference> Files { get; set; } = default!;
}