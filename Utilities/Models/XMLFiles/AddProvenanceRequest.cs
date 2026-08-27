using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class AddProvenanceRequest
{
    [Display("File", Description = "File in any format supported by Blackbird Filters.")]
    public FileReference File { get; set; } = new();

    [Display("Provenance type", Description = "Whether provenance describes translation or review.")]
    [StaticDataSource(typeof(ProvenanceTypeDataSourceHandler))]
    public string ProvenanceType { get; set; } = string.Empty;

    [Display("Person", Description = "Person name.")]
    public string? Person { get; set; }

    [Display("Person reference", Description = "Person IRI, preferably a URL.")]
    public string? PersonReference { get; set; }

    [Display("Organization", Description = "Organization name.")]
    public string? Organization { get; set; }

    [Display("Organization reference", Description = "Organization IRI, preferably a URL.")]
    public string? OrganizationReference { get; set; }

    [Display("Tool", Description = "Tool name.")]
    public string? Tool { get; set; }

    [Display("Tool reference", Description = "Tool IRI, preferably a URL.")]
    public string? ToolReference { get; set; }
}
