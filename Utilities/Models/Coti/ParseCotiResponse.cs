using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Coti;

public class ParseCotiResponse
{
    [Display("Source languages")]
    public List<string> SourceLanguages { get; set; } = [];

    [Display("Target languages")]
    public List<string> TargetLanguages { get; set; } = [];

    [Display("Translation files")]
    public List<CotiTranslationFile> TranslationFiles { get; set; } = [];

    [Display("Packaged reference files")]
    public List<CotiReferenceFile> ReferenceFiles { get; set; } = [];
}
