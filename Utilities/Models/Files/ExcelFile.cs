using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files;
public class ExcelFile
{
    [Display("Spreadsheet file")]
    public FileReference File { get; set; }
}
