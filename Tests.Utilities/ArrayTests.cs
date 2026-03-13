using Apps.Utilities.Actions;
using Apps.Utilities.Models.Arrays.Request;
using Apps.Utilities.Models.Texts;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class ArrayTests : TestBase
{
    [TestMethod]
    public void Array_intersect_works()
    {
        var array1 = new List<string> { "one", "two", "three" };
        var array2 = new List<string> { "two", "three", "four" };

        var actions = new Arrays(InvocationContext);
        var result = actions.ArrayIntersect(new ArrayIntersectionRequest { FirstArray = array1, SecondArray = array2 });

        Assert.IsTrue(result.Array.Contains("two") && result.Array.Contains("three") && result.Array.Count() == 2);
    }

    [TestMethod]
    public void ExtractArrayUsingRegex_ReturnsCountWords()
    {
        var input = new TextsDto { Texts = ["en", "DE", "fr-CH", "ES_mx", "en-US", "EN_gb", "eng", "eng-US"] };

        var regex = new RegexInput
        {
            Regex = @"^(?:en|eng)(?:[-_][A-Za-z0-9]{2,8})*$",
            Flags = ["insensitive"]
        };
        var actions = new Arrays(InvocationContext);
        var result = actions.ExtractArrayUsingRegex(input, regex);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        Console.WriteLine(json);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ArrayFilter_works()
    {
        var array1 = new List<string> { "one", "two", "three" };
        var array2 = new List<string> { "two", "three", "four" };

        var actions = new Arrays(InvocationContext);
        var result = actions.ArrayFilter(new ArrayFilterRequest { Control = ["Location"], Array = new[] { "test", "Location" } });
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));

        Assert.IsNotNull(result);
    }
}