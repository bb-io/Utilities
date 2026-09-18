using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.Models.Files;

public class ChangeFileEncodingRequest : FileDto
{
    [Display("Target encoding")]
    [StaticDataSource(typeof(EncodingSourceHandler))]
    public string TargetEncoding { get; set; } = string.Empty;

    [Display("Source encoding", Description = "Optional. If omitted, detects encoding from BOM or uses UTF-8 when no BOM is present.")]
    [StaticDataSource(typeof(EncodingSourceHandler))]
    public string? SourceEncoding { get; set; }
}
