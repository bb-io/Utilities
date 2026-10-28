using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class TranslateXliffFromTranslationMemoryResponse
{
    [Display("Translated XLIFF file")]
    public required FileReference File { get; set; }

    [Display("Segments translated")]
    public int SegmentsTranslated { get; set; }

    [Display("Segments without a match")]
    public int SegmentsWithoutMatch { get; set; }

    [Display("Segments skipped")]
    public int SegmentsSkipped { get; set; }

    [Display("Warnings")]
    public List<string> Warnings { get; set; } = [];
}
