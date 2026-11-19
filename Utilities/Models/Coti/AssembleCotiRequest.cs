using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Coti;

public class AssembleCotiRequest
{
    [Display("Original COTI file")]
    public FileReference OriginalCotiFile { get; set; } = default!;

    [Display("Translated files", Description = "Translated content files. Provide the matching original archive paths in the same order.")]
    public IEnumerable<FileReference> TranslatedFiles { get; set; } = [];

    [Display("Original archive paths", Description = "Original archive paths returned by Parse COTI file, in the same order as the translated files.")]
    public IEnumerable<string> OriginalArchivePaths { get; set; } = [];
}
