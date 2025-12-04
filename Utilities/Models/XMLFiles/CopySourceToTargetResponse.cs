using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class CopySourceToTargetResponse
{
    public required FileReference File { get; set; }

    [Display("Segments copied")]
    public int SegmentsCopied { get; set; }
}
