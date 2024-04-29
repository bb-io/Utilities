using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.Models.Files;

public class ConvertTextToDocumentRequest
{
    public string Text { get; set; }
    [Display("File Name")]
    public string Filename { get; set; }
    [Display("File Extension")]
    [DataSource(typeof(ExtensionSourceHandler))]
    public string FileExtension { get; set; }
}