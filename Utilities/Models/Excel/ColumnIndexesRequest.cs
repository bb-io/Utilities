using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Excel;

public class ColumnIndexesRequest
{
    [Display("Column indexes", Description = "The first column starts with 1")]
    public IEnumerable<int> ColumnIndexes { get; set; } = default!;
}
