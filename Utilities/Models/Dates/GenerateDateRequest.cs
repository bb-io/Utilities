using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.Models.Dates;

public class GenerateDateRequest
{
    [Display("Add days")]
    public double? AddDays { get; set; }

    [Display("Add business days")]
    public double? BusinessDays { get; set; }

    [Display("Add hours")]
    public double? AddHours { get; set; }

    [Display("Add minutes")]
    public double? AddMinutes { get; set; }

    [Display("Date")]
    public DateTime? Date { get; set; }

    [Display("Timezone")]
    [StaticDataSource(typeof(TimeZoneSourceHandler))]
    public string? Timezone { get; set; }
}