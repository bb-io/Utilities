using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Files;

public class RenameRequest
{
    [Display("New name")]
    public string Name { get; set; }
}