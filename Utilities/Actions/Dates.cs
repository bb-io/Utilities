using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Globalization;
using Apps.Utilities.Models.Dates;
using CsvHelper;

namespace Apps.Utilities.Actions;

[ActionList]
public class Dates : BaseInvocable
{
    public Dates(InvocationContext context) : base(context) { }

    [Action("Generate date", Description = "Generates a date relative to the moment this action is called or relative to a custom date.")]
    public DateResponse GenerateDate([ActionParameter] GenerateDateRequest input)
    {
        var referenceDate = input.Date ?? DateTime.Now;

        if (input.BusinessDays.HasValue)
            referenceDate = AddBusinessDays(referenceDate, (int) input.BusinessDays.Value);
    
        return new DateResponse { Date = referenceDate.AddDays(input.AddDays ?? 0).AddHours(input.AddHours ?? 0).AddMinutes(input.AddMinutes ?? 0) };
    }

    [Action("Get first day of previous month", Description = "Generates a date corresponding to the first day of the previous month.")]
    public DateResponse FirstDayLastMonth()
    {
        var today = DateTime.Today;
        var month = new DateTime(today.Year, today.Month, 1);
        var first = month.AddMonths(-1);

        return new DateResponse { Date = first};
    }

    [Action("Get last day of previous month", Description = "Generates a date corresponding to the last day of the previous month.")]
    public DateResponse LastDayLastMonth()
    {
        var today = DateTime.Today;
        var month = new DateTime(today.Year, today.Month, 1);
        var last = month.AddDays(-1);

        return new DateResponse { Date =  last};
    }

    [Action("Format date", Description = "Formats a date to text according to pre-defined formatting rules and culture")]
    public FormattedDateResponse FormatDate([ActionParameter] FormatDateRequest input )
    {
        return new FormattedDateResponse { FormattedDate = input.Date.ToString(input.Format, input.Culture != null ? new CultureInfo(input.Culture) : CultureInfo.InvariantCulture) };
    }

    [Action("Get date difference", Description = "Returns the difference between the two inputted days in total seconds, minutes, hours and days.")]
    public DateDifferenceResponse DateDifference([ActionParameter][Display("Date 1")] DateTime date1, [ActionParameter][Display("Date 2")] DateTime date2)
    {
        return new DateDifferenceResponse
        {
            Seconds = Math.Abs((date2 - date1).TotalSeconds),
            Minutes = Math.Abs((date2 - date1).TotalMinutes),
            Hours = Math.Abs((date2 - date1).TotalHours),
            Days = Math.Abs((date2 - date1).TotalDays),
        };
    }

    [Action("Convert text to date", Description = "Converts text input to date.")]
    public DateResponse ConvertTextToDate([ActionParameter] TextToDateRequest input)
    {
        var culture = input.Culture != null
        ? new CultureInfo(input.Culture)
        : CultureInfo.InvariantCulture;
        var parsed = DateTime.Parse(input.Text, culture, DateTimeStyles.None);

        if (!string.IsNullOrEmpty(input.Timezone))
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(input.Timezone);

            var offset = tz.GetUtcOffset(parsed);

            var dateUnspecified = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);

            return new DateResponse { Date = dateUnspecified };
        }
        else
        {
            var dateUnspecified = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
            return new DateResponse { Date = dateUnspecified };
        }
    }

    private static DateTime AddBusinessDays(DateTime date, int days)
    {
        if (days < 0)
        {
            throw new ArgumentException("days cannot be negative", "days");
        }

        if (days == 0) return date;

        if (date.DayOfWeek == DayOfWeek.Saturday)
        {
            date = date.AddDays(2);
            days -= 1;
        }
        else if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
            days -= 1;
        }

        date = date.AddDays(days / 5 * 7);
        int extraDays = days % 5;

        if ((int)date.DayOfWeek + extraDays > 5)
        {
            extraDays += 2;
        }

        return date.AddDays(extraDays);

    }
}