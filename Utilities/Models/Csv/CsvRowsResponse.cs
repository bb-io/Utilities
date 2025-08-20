using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Csv;
public class CsvRowsResponse
{
    public IEnumerable<Row> Rows { get; set; }

    public double TotalRows { get; set; }
}

public class Row 
{
    [Display("Row ID")]
    public string Id { get; set; }

    [Display("Cell values")]
    public List<string> Values { get; set; }
}