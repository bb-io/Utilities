using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class ApplyXliffTargetTranslationsResponse
{
    [Display("Updated target XLIFF file")]
    public required FileReference File { get; set; }

    [Display("Warnings")]
    public List<string> Warnings { get; set; } = [];
}
