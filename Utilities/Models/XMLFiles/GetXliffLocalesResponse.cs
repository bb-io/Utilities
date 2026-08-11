using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.XMLFiles;

public class GetXliffLocalesResponse
{
    [Display("Source locale")]
    public string? SourceLocale { get; set; }

    [Display("Target locale")]
    public string? TargetLocale { get; set; }
}
