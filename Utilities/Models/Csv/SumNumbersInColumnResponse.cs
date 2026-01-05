using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Csv
{
    public class SumNumbersInColumnResponse
    {
        public double Sum { get; set; }

        [Display("From row used")]
        public int FromRowUsed { get; set; }

        [Display("To row used")]
        public int ToRowUsed { get; set; }

        [Display("Rows processed")]
        public int RowsProcessed { get; set; }
    }
}
