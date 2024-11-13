using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Files;

public class FilesWordCountResponse
{
    [Display("Total word count")]
    public double WordCount { get; set; }

    [Display("Files with word count")]
    public List<WordCountItem> FilesWithWordCount { get; set; } = new();
}

public class WordCountItem
{
    [Display("File name")]
    public string FileName { get; set; } = string.Empty;

    [Display("Word count")]
    public double WordCount { get; set; }
}