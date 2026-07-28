using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class SetXliffLocalesRequest
{
    [Display("File", Description = "XLIFF file or another file type supported by Blackbird Filters.")]
    public FileReference File { get; set; } = new();

    [Display("New source locale", Description = "New source locale. Leave empty to keep the current locale.")]
    public string? NewSourceLocale { get; set; }

    [Display("New target locale", Description = "New target locale. Leave empty to keep the current locale.")]
    public string? NewTargetLocale { get; set; }
}
