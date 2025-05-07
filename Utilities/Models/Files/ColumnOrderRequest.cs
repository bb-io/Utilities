using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Files;
public class ColumnOrderRequest
{
    [Display("New columns", Description = "0 being the first column. A value of [1, 1, 2] would indicate that there are 3 columns in the new CSV file. The first two columns would have the value of the original column 1, the third column would have original column 2.")]
    public IEnumerable<int> ColumnOrder { get; set; }
}
