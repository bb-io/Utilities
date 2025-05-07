using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Files;
public class ExcelColumnOrderRequest
{
    [Display("New columns", Description = "1 being the first column. A value of [1, 1, 2] would indicate that there will be 3 columns in the resulting Excel file. The first two columns would have the value of the original column 1, the third column would have original column 2.")]
    public IEnumerable<int> ColumnOrder { get; set; }
}
