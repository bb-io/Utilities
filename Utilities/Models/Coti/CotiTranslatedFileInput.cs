using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Coti;

public class CotiTranslatedFileInput
{
    [Display("File")]
    public FileReference File { get; set; } = default!;

    [Display("Original archive path")]
    public string OriginalArchivePath { get; set; } = string.Empty;
}
