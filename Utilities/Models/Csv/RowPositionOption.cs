using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Csv;

public class RowPositionOption
{
    [Display("Row position")]
    public int? RowPosition { get; set; }

    [Display("Input values")]
    public IEnumerable<string> InputValues { get; set; }
}
