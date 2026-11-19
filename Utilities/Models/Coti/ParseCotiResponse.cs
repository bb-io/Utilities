using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Coti;

public class ParseCotiResponse
{
    [Display("Source languages")]
    public IEnumerable<string> SourceLanguages { get; set; } = [];

    [Display("Target languages")]
    public IEnumerable<string> TargetLanguages { get; set; } = [];

    [Display("Translation files")]
    public IEnumerable<FileReference> TranslationFiles { get; set; } = [];

    [Display("Translation file archive paths", Description = "Archive paths in the same order as Translation files.")]
    public IEnumerable<string> TranslationFileArchivePaths { get; set; } = [];

    [Display("Translation file details", Description = "Project and language metadata in the same order as Translation files.")]
    public IEnumerable<CotiTranslationFile> TranslationFileDetails { get; set; } = [];

    [Display("Packaged reference files")]
    public IEnumerable<FileReference> ReferenceFiles { get; set; } = [];

    [Display("Reference file archive paths", Description = "Archive paths in the same order as Packaged reference files.")]
    public IEnumerable<string> ReferenceFileArchivePaths { get; set; } = [];

    [Display("Reference file details", Description = "Manifest metadata in the same order as Packaged reference files.")]
    public IEnumerable<CotiReferenceFile> ReferenceFileDetails { get; set; } = [];
}
