using Tests.Utilities.Base;
using Apps.Utilities.Actions;
using Apps.Utilities.Models.Dates;

namespace Tests.Utilities;

[TestClass]
public class DateTests : TestBase
{
    [TestMethod]
    public async Task ConvertTextToDate_IssSuccess()
    {
        var action = new Dates(InvocationContext);

        var request = new TextToDateRequest
        {
            Text = "10/10/2025",
            //Format = "dd/MM/yyyy ",
            Timezone = "UTC"
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
