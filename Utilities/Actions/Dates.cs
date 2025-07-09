using Apps.Utilities.DataSourceHandlers;
using Apps.Utilities.Models.Dates;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json;
using System.Globalization;

namespace Apps.Utilities.Actions;

[ActionList("Dates")]
public class Dates : BaseInvocable
{
    public Dates(InvocationContext context) : base(context) { }

    [Action("Generate date", Description = "Generates a date relative to the moment this action is called or relative to a custom date.")]
    public DateResponse GenerateDate([ActionParameter] GenerateDateRequest input)
    {
        try
        {
            var referenceDate = input.Date ?? DateTime.Now;

            if (input.BusinessDays.HasValue)
                referenceDate = AddBusinessDays(referenceDate, (int)input.BusinessDays.Value);

            var adjustedDate = referenceDate
                .AddDays(input.AddDays ?? 0)
                .AddHours(input.AddHours ?? 0)
                .AddMinutes(input.AddMinutes ?? 0);

            var dateTimeOffset = CreateDateTimeOffset(adjustedDate, input.Timezone);

            var utcDate = dateTimeOffset.UtcDateTime;
            return new DateResponse { Date = DateTime.SpecifyKind(utcDate, DateTimeKind.Utc) };
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new PluginApplicationException($"Timezone '{input.Timezone}' not recognized.", ex);
        }
    }

    [Action("Get first day of previous month", Description = "Generates a date corresponding to the first day of the previous month.")]
    public DateResponse FirstDayLastMonth()
    {
        var today = DateTime.Today;
        var month = new DateTime(today.Year, today.Month, 1);
        var first = month.AddMonths(-1);

        return new DateResponse { Date = first };
    }

    [Action("Get last day of previous month", Description = "Generates a date corresponding to the last day of the previous month.")]
    public DateResponse LastDayLastMonth()
    {
        var today = DateTime.Today;
        var month = new DateTime(today.Year, today.Month, 1);
        var last = month.AddDays(-1);

        return new DateResponse { Date = last };
    }

    [Action("Format date", Description = "Formats a date to text according to pre-defined formatting rules and culture")]
    public FormattedDateResponse FormatDate([ActionParameter] FormatDateRequest input)
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
        try
        {
            var culture = !string.IsNullOrEmpty(input.Culture)
                ? new CultureInfo(input.Culture)
                : CultureInfo.InvariantCulture;

            DateTimeOffset finalResult;

            if (!string.IsNullOrEmpty(input.Format))
            {
                finalResult = ParseWithSpecificFormat(input.Text, input.Format, culture, input.Timezone);
            }
            else
            {
                finalResult = ParseWithAutoDetection(input.Text, culture, input.Timezone);
            }

            var inputDateTime = string.IsNullOrEmpty(input.Format)
                ? DateTime.Parse(input.Text, culture)
                : DateTime.ParseExact(input.Text, input.Format, culture);
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(input.Timezone);
            var expectedDateTimeOffset = new DateTimeOffset(inputDateTime, tzInfo.GetUtcOffset(inputDateTime));
            var expectedUtc = expectedDateTimeOffset.ToUniversalTime();

            var actualUtc = finalResult.ToUniversalTime();

            var offsetDifference = (expectedUtc - actualUtc).TotalHours;

            var correctedUtc = actualUtc.AddHours(offsetDifference).DateTime;

            return new DateResponse { Date = DateTime.SpecifyKind(correctedUtc, DateTimeKind.Utc) };
        }
        catch (FormatException ex)
        {
            var formatInfo = !string.IsNullOrEmpty(input.Format)
                ? $"Expected format: '{input.Format}'. "
                : "Auto-detection failed. ";
            throw new PluginApplicationException(
                $"Invalid date format provided in the input text. {formatInfo}Input: '{input.Text}'.", ex);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new PluginApplicationException($"Timezone '{input.Timezone}' not recognized.", ex);
        }
    }

    private static DateTime AddBusinessDays(DateTime date, int days)
    {
        if (days < 0)
        {
            return SubtractBusinessDays(date, -days);
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

    private static DateTime SubtractBusinessDays(DateTime date, int days)
    {
        if (days == 0)
            return date;

        if (date.DayOfWeek == DayOfWeek.Saturday)
        {
            date = date.AddDays(-1);
            days -= 1;
        }
        else if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            date = date.AddDays(-2);
            days -= 1;
        }

        date = date.AddDays(-(days / 5) * 7);

        int extraDays = days % 5;
        if ((int)date.DayOfWeek - extraDays < 1)
            extraDays += 2;

        return date.AddDays(-extraDays);
    }

    private DateTimeOffset ParseWithSpecificFormat(string text, string format, CultureInfo culture, string timezone)
    {
        if (format.Contains("zzz") || format.Contains("zz") || format.Contains("z"))
        {
            var parsedDto = DateTimeOffset.ParseExact(text, format, culture, DateTimeStyles.None);

            if (!string.IsNullOrEmpty(timezone))
            {
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                return TimeZoneInfo.ConvertTime(parsedDto, tzInfo);
            }

            return parsedDto;
        }
        else
        {
            var parsed = DateTime.ParseExact(text, format, culture, DateTimeStyles.None);
            return CreateDateTimeOffset(parsed, timezone);
        }
    }

    private DateTimeOffset ParseWithAutoDetection(string text, CultureInfo culture, string timezone)
    {
        var formatHandler = new DateFormatSourceHandler();
        var allFormats = formatHandler.GetData().Select(item => item.Value).ToArray();

        var timezoneFormats = allFormats.Where(f => f.Contains("zzz") || f.Contains("zz") || f.Contains("z")).ToArray();
        var regularFormats = allFormats.Where(f => !f.Contains("zzz") && !f.Contains("zz") && !f.Contains("z")).ToArray();

        if (DateTimeOffset.TryParseExact(text, timezoneFormats, culture, DateTimeStyles.None, out var parsedWithTz))
        {
            if (!string.IsNullOrEmpty(timezone))
            {
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                return TimeZoneInfo.ConvertTime(parsedWithTz, tzInfo);
            }
            return parsedWithTz;
        }


        if (DateTimeOffset.TryParse(text, culture, DateTimeStyles.None, out var generalParsedDto))
        {
            if (!string.IsNullOrEmpty(timezone))
            {
                var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                return TimeZoneInfo.ConvertTime(generalParsedDto, tzInfo);
            }
            return generalParsedDto;
        }
        if (DateTime.TryParseExact(text, regularFormats, culture, DateTimeStyles.None, out var parsed))
        {
            return CreateDateTimeOffset(parsed, timezone);
        }
        var lastResort = DateTime.Parse(text, culture, DateTimeStyles.None);
        return CreateDateTimeOffset(lastResort, timezone);
    }

    private DateTimeOffset CreateDateTimeOffset(DateTime dateTime, string timezone)
    {
        if (!string.IsNullOrEmpty(timezone))
        {
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            var unspecified = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
            var offset = tzInfo.GetUtcOffset(unspecified);
            return new DateTimeOffset(unspecified, offset);
        }

        var localOffset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
        return new DateTimeOffset(dateTime, localOffset);
    }
}
