using Apps.Utilities.Actions;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class TextTests : TestBase
{
    private Texts _textActions;

    [TestInitialize]
    public void Init()
    {
        var outputDirectory = Path.Combine(GetTestFolderPath(), "Output");
        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, true);
        }
        
        Directory.CreateDirectory(outputDirectory);

        _textActions = new Texts(InvocationContext);
    }

    [TestMethod]
    public void TrimText_TrimSizeLongerThanText_ReturnsEmptyString()
    {
        var text = "not a very long text";

        var result = _textActions.TrimText(new TextDto { Text = text},new TrimTextInput { CharactersFromEnd = 60000});

        Assert.IsTrue(result.Length == 0);
    }
    
    [TestMethod]
    public void SplitStringToArray_CommaSeparated_ReturnsCorrectArray()
    {
        var textDto = new TextDto { Text = "en,de,fr"};
        var expected = new List<string> { "en", "de", "fr" };

        var result = _textActions.SplitStringToArray(textDto, new() { Delimiter = ","});

        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void SplitStringToArray_CustomDelimiter_ReturnsCorrectArray()
    {
        var textDto = new TextDto { Text = "apple | banana | cherry"};
        var expected = new List<string> { "apple", "banana", "cherry" };

        var result = _textActions.SplitStringToArray(textDto, new() { Delimiter = " | "});

        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    public void SplitStringToArray_HandlesExtraSpaces_ReturnsTrimmedArray()
    {
        var textDto = new TextDto { Text = "  en , de ,  fr  "};
        var expected = new List<string> { "en", "de", "fr" };

        var result = _textActions.SplitStringToArray(textDto, new() { Delimiter = ","});

        CollectionAssert.AreEqual(expected, result);
    }

    [TestMethod]
    [ExpectedException(typeof(PluginMisconfigurationException))]
    public void SplitStringToArray_EmptyString_ThrowsException()
    {
        _textActions.SplitStringToArray(new(), new() { Delimiter = ","});
    }


    [TestMethod]
    public void CountWordsInTextFromArray_ReturnsCountWords()
    {
        int expected = 7;
        var textDto = new TextsDto { Texts = ["hello","world","my friend", "how, are you?"] };
        var result = _textActions.CountWordsInTextFromArray(textDto);
        Console.WriteLine(result);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void GenerateRandomText_Works()
    {
        var result = _textActions.GenerateRandomText(null, null);
        Console.WriteLine(result);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void GenerateRandomText_with_charcount_Works()
    {
        var result = _textActions.GenerateRandomText(15, null);
        Console.WriteLine(result);
        Assert.IsTrue(result.Length == 15);
    }

    [TestMethod]
    public void GenerateRandomText_with_charset_Works()
    {
        var chars = "abcdef";
        var result = _textActions.GenerateRandomText(null, chars);
        Console.WriteLine(result);
        foreach (char c in result)
        {
            Assert.IsTrue(chars.Contains(c));
        }
        
    }
}