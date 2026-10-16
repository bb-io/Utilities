using Tests.Utilities.Base;
using Apps.Utilities.Actions;
using Apps.Utilities.Models.Dates;

namespace Tests.Utilities;

[TestClass]
public class DateTests : TestBase
{
    [TestMethod]
    public void ConvertTextToDate_IssSuccess()
    {
        var action = new Dates(InvocationContext);

        var request = new TextToDateRequest
        {
            Text = "04/08/2026 03:12:54 +00:00",
            Timezone = "America/Ciudad_Juarez"
        };

        var result = action.ConvertTextToDate(request);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
        Console.WriteLine(json);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ConvertTextToDate_ThrowsMisconfiguration_WhenTextIsNull()
    {
        var action = new Dates(InvocationContext);
        var request = new TextToDateRequest { Text = null! };

        var exception = Assert.ThrowsException<Blackbird.Applications.Sdk.Common.Exceptions.PluginMisconfigurationException>(
            () => action.ConvertTextToDate(request));

        Assert.AreEqual("Text is required and cannot be null or empty.", exception.Message);
    }

    [TestMethod]
    public async Task GenerateDate_IssSuccess()
    {
        var action = new Dates(InvocationContext);

        var request = new GenerateDateRequest
        {
            Timezone = "America/St_Johns"
        };

        var result = action.GenerateDate(request);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
        Console.WriteLine(json);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void GenerateDate_SetTimeWithTimezone_IsAppliedInThatTimezone()
    {
        var action = new Dates(InvocationContext);

        // Thursday Oct 8, 15:51 CDT (UTC-5) + 2 business days at 17:00 CT
        var result = action.GenerateDate(new GenerateDateRequest
        {
            Date = new DateTime(2026, 10, 8, 20, 51, 0, DateTimeKind.Utc),
            BusinessDays = 2,
            Timezone = "America/Chicago",
            FixedTime = "17:00"
        });

        Assert.AreEqual(new DateTime(2026, 10, 12, 22, 0, 0, DateTimeKind.Utc), result.Date);
        Assert.AreEqual(DateTimeKind.Utc, result.Date.Kind);
    }

    [TestMethod]
    public void GenerateDate_Timezone_UsesTimezoneDayNotUtcDay()
    {
        var action = new Dates(InvocationContext);

        // Thursday Oct 8, 20:00 CDT is already Friday Oct 9 in UTC
        var result = action.GenerateDate(new GenerateDateRequest
        {
            Date = new DateTime(2026, 10, 9, 1, 0, 0, DateTimeKind.Utc),
            BusinessDays = 2,
            Timezone = "America/Chicago",
            FixedTime = "17:00"
        });

        Assert.AreEqual(new DateTime(2026, 10, 12, 22, 0, 0, DateTimeKind.Utc), result.Date);
    }

    [TestMethod]
    public void GenerateDate_Timezone_RespectsDaylightSavingChange()
    {
        var action = new Dates(InvocationContext);

        // After Nov 1 Central Time is CST (UTC-6)
        var result = action.GenerateDate(new GenerateDateRequest
        {
            Date = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc),
            BusinessDays = 1,
            Timezone = "America/Chicago",
            FixedTime = "17:00"
        });

        Assert.AreEqual(new DateTime(2026, 11, 4, 23, 0, 0, DateTimeKind.Utc), result.Date);
    }

    [TestMethod]
    public void GenerateDate_SetTimeWithoutTimezone_IsUtc()
    {
        var action = new Dates(InvocationContext);

        var result = action.GenerateDate(new GenerateDateRequest
        {
            Date = new DateTime(2026, 10, 8, 20, 51, 0, DateTimeKind.Utc),
            FixedTime = "17:00"
        });

        Assert.AreEqual(new DateTime(2026, 10, 8, 17, 0, 0, DateTimeKind.Utc), result.Date);
    }

    [TestMethod]
    public void GenerateDate_UnspecifiedDateWithTimezone_IsTreatedAsTimezoneTime()
    {
        var action = new Dates(InvocationContext);

        var result = action.GenerateDate(new GenerateDateRequest
        {
            Date = new DateTime(2026, 10, 8, 10, 0, 0, DateTimeKind.Unspecified),
            Timezone = "America/Chicago"
        });

        Assert.AreEqual(new DateTime(2026, 10, 8, 15, 0, 0, DateTimeKind.Utc), result.Date);
    }

    [TestMethod]
    public void GenerateDate_NowWithTimezone_ReturnsCurrentMoment()
    {
        var action = new Dates(InvocationContext);

        var result = action.GenerateDate(new GenerateDateRequest { Timezone = "America/Chicago" });

        Assert.IsTrue(Math.Abs((result.Date - DateTime.UtcNow).TotalMinutes) < 1);
    }
}
