using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.Models.Files;

public class ConvertTextToDocumentRequest
{
    public string Text { get; set; }
    
    [Display("File Name")]
    public string Filename { get; set; }
    
    [Display("File Extension")]
    [StaticDataSource(typeof(ExtensionSourceHandler))]
    public string FileExtension { get; set; }
    
    [Display("Font", Description = "By default, the font is set to Arial."), StaticDataSource(typeof(FontStaticDataSourceHandler))]
    public string? Font { get; set; }
    
    [Display("Font size", Description = "By default, the font size is set to 12.")]
    public int? FontSize { get; set; }

    [Display("Encoding", Description = "Text encoding for txt, html, or csv files. Default is UTF-8 without BOM.")]
    [StaticDataSource(typeof(EncodingSourceHandler))]
    public string? Encoding { get; set; }
}