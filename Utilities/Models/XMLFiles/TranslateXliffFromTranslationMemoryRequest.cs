using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class TranslateXliffFromTranslationMemoryRequest
{
    [Display("XLIFF file", Description = "XLIFF file whose targets will be translated.")]
    public FileReference XliffFile { get; set; } = new();

    [Display("Translation memory", Description = "Translation memory in TMX format.")]
    public FileReference TranslationMemory { get; set; } = new();

    [Display("Source language", Description = "Source language to select from the TMX file. Defaults to the XLIFF source language.")]
    public string? SourceLanguage { get; set; }

    [Display("Target language", Description = "Target language to select from the TMX file. Defaults to the XLIFF target language.")]
    public string? TargetLanguage { get; set; }

    [Display("Ignore source case", Description = "Match XLIFF and TMX sources without considering letter case. Defaults to false.")]
    public bool? IgnoreSourceCase { get; set; }
}
