using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Coti;

public class CotiTranslationFile
{
    [Display("File")]
    public FileReference File { get; set; } = default!;

    [Display("Original archive path")]
    public string OriginalArchivePath { get; set; } = string.Empty;

    [Display("Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    [Display("Source language")]
    public string SourceLanguage { get; set; } = string.Empty;

    [Display("Target language")]
    public string TargetLanguage { get; set; } = string.Empty;
}
