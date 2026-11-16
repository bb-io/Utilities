using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class ApplyXliffTargetTranslationsRequest
{
    [Display("Target XLIFF file", Description = "XLIFF file whose target translations will be updated.")]
    public FileReference TargetFile { get; set; } = new();

    [Display("Translations XLIFF file", Description = "XLIFF file supplying target translations.")]
    public FileReference TranslationsFile { get; set; } = new();

    [Display("Segment states", Description = "Only copy translations whose segments in the translations XLIFF file are in one of these states. Leave empty to copy all states. Segments without a state are treated as initial.")]
    [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
    public IEnumerable<string>? SegmentStates { get; set; }

    [Display("Copy provenance metadata", Description = "Copy unit-level translation and review provenance from matched units. Defaults to false.")]
    public bool? CopyProvenanceMetadata { get; set; }

    [Display("Copy quality data", Description = "Copy unit-level quality ratings from matched units. Defaults to false.")]
    public bool? CopyQualityData { get; set; }
}
