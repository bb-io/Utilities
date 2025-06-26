using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Polling.Models.Request;

public class TimeIntervalPassedRequest
{
    [Display("Active from time (HH:mm)", Description = "Only trigger if the current time is after this time (HH:mm in UTC)")]
    public string? ActiveFromTime { get; set; }

    [Display("Active until time (HH:mm)", Description = "Only trigger if the current time is before this time (HH:mm in UTC)")]
    public string? ActiveUntilTime { get; set; }
}