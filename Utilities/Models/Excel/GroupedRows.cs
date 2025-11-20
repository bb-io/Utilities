using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Excel;

public class GroupedRows
{
    [Display("Group key")]
    public string Key { get; set; }

    [Display("Rows")]
    public List<Row> Rows { get; set; }

}

public class Row
{
    public List<string> Cells { get; set; }
}
