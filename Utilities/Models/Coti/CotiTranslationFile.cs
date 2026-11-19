using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Coti;

public class CotiTranslationFile
{
    [Display("Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    [Display("Source language")]
    public string SourceLanguage { get; set; } = string.Empty;

    [Display("Target language")]
    public string TargetLanguage { get; set; } = string.Empty;
}
