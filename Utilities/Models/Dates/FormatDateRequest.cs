using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Apps.Utilities.DataSourceHandlers;

namespace Apps.Utilities.Models.Dates;

public class FormatDateRequest
{
    [Display("Date")]
    public DateTime Date { get; set; }

    [Display("Format")]
    [DataSource(typeof(DateFormatSourceHandler))]
    public string Format { get; set; }

    [Display("Culture")]
    [DataSource(typeof(CultureSourceHandler))]
    public string? Culture { get; set; }
}