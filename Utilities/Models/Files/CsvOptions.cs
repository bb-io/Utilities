using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Files;

public class CsvOptions
{
    [Display("Newline characters", Description = "Default '\\r\\n'")]
    public string? NewLine { get; set; }

    [Display("Delimiter", Description = "Default ','")]
    public string? Delimiter { get; set; }

    [Display("Comment character", Description = "Default '#'")]
    public string? Comment { get; set; }

    [Display("Escape character", Description = "Default '\"'")]
    public string? Escape { get; set; }

    [Display("Quote character", Description = "Default '\"'")]
    public string? Quote { get; set; }

    [Display("Has header?", Description = "Default true")]
    public bool? HasHeader { get; set; }

    [Display("Ignore blank lines?", Description = "Default true")]
    public bool? IgnoreBlankLines { get; set; }
}
