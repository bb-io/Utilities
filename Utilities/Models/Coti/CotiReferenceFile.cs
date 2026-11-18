using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Coti;

public class CotiReferenceFile
{
    [Display("File")]
    public FileReference File { get; set; } = default!;

    [Display("Original archive path")]
    public string OriginalArchivePath { get; set; } = string.Empty;

    [Display("Project ID")]
    public string ProjectId { get; set; } = string.Empty;

    [Display("Manifest file reference")]
    public string ManifestFileReference { get; set; } = string.Empty;

    [Display("External")]
    public string External { get; set; } = "no";

    [Display("MIME type")]
    public string MimeType { get; set; } = string.Empty;

    [Display("Encoding")]
    public string? Encoding { get; set; }

    [Display("Type")]
    public string? Type { get; set; }

    [Display("Description")]
    public string? Description { get; set; }

    [Display("TMS ID")]
    public string? TmsId { get; set; }
}
