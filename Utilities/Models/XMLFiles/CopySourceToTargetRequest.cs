using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class CopySourceToTargetRequest
{
    [Display("XLIFF file")]
    public FileReference File { get; set; } = new();
}
