using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Excel;

public class ExcelGroupingRequest
{
    [Display("Sheet number")]
    public int WorksheetIndex { get; set; }

    [Display("Column index (1-based)")]
    public int ColumnIndex { get; set; }

    [Display("Skip first row (header)")]
    public bool SkipHeader { get; set; }

}
