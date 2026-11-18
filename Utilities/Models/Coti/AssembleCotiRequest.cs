using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Coti;

public class AssembleCotiRequest
{
    [Display("Original COTI file")]
    public FileReference OriginalCotiFile { get; set; } = default!;

    [Display("Translated files", Description = "Translated files returned by Parse COTI file, with their original archive paths.")]
    public IEnumerable<CotiTranslatedFileInput> TranslatedFiles { get; set; } = [];
}
