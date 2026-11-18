using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Coti;

public class AssembleCotiResponse
{
    [Display("COTI file")]
    public FileReference File { get; set; } = default!;
}
