using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Excel;

public class RowIndexesRequest
{
    [Display("Row indexes", Description = "The first row starts with 1")]
    public IEnumerable<int> RowIndexes { get; set; } = default!;
}
