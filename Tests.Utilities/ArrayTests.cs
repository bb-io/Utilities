using Apps.Utilities.Actions;
using Apps.Utilities.Models.Arrays.Request;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common.Exceptions;
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
    public async Task ExtractArrayUsingRegex_ReturnsCountWords()
    {
        //var input = new TextsDto { Texts = ["hello", "world", "my friend", "how, are you?"] };
        //var regex = new RegexInput
        //{
        //    Regex = @"(?<word>[A-Za-z]+)",
        //    Group = "word"
        //};

        //var input = new TextsDto
        //{
        //    Texts = new List<string> { "en", "EN-us", "de-DE", "pt_BR", "xx", "123", "zh-Hant-HK" }
        //};
        //var regex = new RegexInput
        //{
        //    Regex = @"^(?<code>[A-Za-z]{2})\b",
        //    Group = "code",
        //    Flags = new List<string> { "insensitive" }
        //};

        //var input = new TextsDto { Texts = new List<string> { "en", "DE", "fr-CH", "ES_mx" } };
        //var regex = new RegexInput
        //{
        //    Regex = @"(?<code>[A-Za-z]{2})",
        //    Group = "code",
        //    Flags = new List<string> { "insensitive" }
        //};

        var input = new TextsDto { Texts = new List<string> { "en", "DE", "fr-CH", "ES_mx", "en-US", "EN_gb", "eng", "eng-US" } };

        var regex = new RegexInput
        {
            Regex = @"^(?:en|eng)(?:[-_][A-Za-z0-9]{2,8})*$",
            Flags = new List<string> { "insensitive" }
        };
        var actions = new Arrays(InvocationContext);
        var result = await actions.ExtractArrayUsingRegex(input, regex);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(result);
        Console.WriteLine(json);
        Assert.IsNotNull(result);
    }
}