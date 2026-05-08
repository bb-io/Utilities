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
}
