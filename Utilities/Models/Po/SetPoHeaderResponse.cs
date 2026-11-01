using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Po;

public class SetPoHeaderResponse
{
    public IEnumerable<PoHeader> Headers { get; set; } = [];
    public FileReference File { get; set; } = null!;
}
