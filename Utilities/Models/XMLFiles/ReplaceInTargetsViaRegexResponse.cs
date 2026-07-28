using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class ReplaceInTargetsViaRegexResponse
{
    [Display("File", Description = "File containing the updated target text.")]
    public required FileReference File { get; set; }
}
