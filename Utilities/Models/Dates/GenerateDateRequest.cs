using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Dates;

public class GenerateDateRequest
{
    [Display("Add days")]
    public double? AddDays { get; set; }

    [Display("Add hours")]
    public double? AddHours { get; set; }

    [Display("Add minutes")]
    public double? AddMinutes { get; set; }

    [Display("Date")]
    public DateTime? Date { get; set; }
}