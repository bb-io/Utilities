using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Excel;

public class CellValuesRequest
{
    [Display("Cell values")]
    public IEnumerable<string> CellValues { get; set; } = default!;
}
