using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Json;

public class ChangeJsonPropertyOutput
{
    [Display("Updated file")]
    public FileReference File { get; set; }
}