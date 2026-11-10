using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files;

public class PseudolocalizeFilesRequest
{
    [Display("Files", Description = "Files supported by Blackbird Filters. Each file is processed separately.")]
    public IEnumerable<FileReference> Files { get; set; } = [];

    [Display("States to pseudolocalize", Description = "Defaults to Initial or empty. Existing segment states are preserved.")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? StatesToPseudolocalize { get; set; }

    [Display("Expansion percentage", Description = "Extra padding as a percentage of translatable source text. Defaults to 0.")]
    public int? ExpansionPercentage { get; set; }
}
