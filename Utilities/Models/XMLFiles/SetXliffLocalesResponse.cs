using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class SetXliffLocalesResponse
{
    [Display("XLIFF file", Description = "XLIFF file containing the resulting source and target locales.")]
    public required FileReference File { get; set; }

    [Display("Source locale changed from")]
    public string? SourceLocaleChangedFrom { get; set; }

    [Display("Source locale changed to")]
    public string? SourceLocaleChangedTo { get; set; }

    [Display("Target locale changed from")]
    public string? TargetLocaleChangedFrom { get; set; }

    [Display("Target locale changed to")]
    public string? TargetLocaleChangedTo { get; set; }
}
