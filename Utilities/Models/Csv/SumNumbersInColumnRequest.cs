using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Csv
{
    public class SumNumbersInColumnRequest
    {
        [Display("Column")]
        public int ColumnIndex { get; set; }

        [Display("From row", Description = "0-based row index. If empty, starts from the first numeric value in the column.")]
        public int? FromRow { get; set; }

        [Display("To row", Description = "0-based row index (inclusive). If empty, sums until the end of file.")]
        public int? ToRow { get; set; }
    }
}
