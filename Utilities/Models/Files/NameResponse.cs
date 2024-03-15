using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Files;

public class NameResponse
{
    [Display("File name without extension")]
    public string NameWithoutExtension { get; set; }

    [Display("File name with extension")]
    public string NameWithExtension { get; set; }

    public string Extension { get; set; }
}