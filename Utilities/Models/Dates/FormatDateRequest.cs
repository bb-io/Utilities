using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.Models.Dates;

public class FormatDateRequest
{
    [Display("Date")]
    public DateTime Date { get; set; }

    [Display("Format")]
    [StaticDataSource(typeof(DateFormatSourceHandler))]
    public string Format { get; set; }

    [Display("Culture")]
    [StaticDataSource(typeof(CultureSourceHandler))]
    public string? Culture { get; set; }
}