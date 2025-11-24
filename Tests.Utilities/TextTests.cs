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
    public void ExtractRegex_ReturnsFirstMatch()
    {
        var result1 = _textActions.ExtractRegex(
            new TextDto { Text = "Hello World 123" },
            new RegexInput { Regex = @"\w+" });
        Console.WriteLine(result1);
        Assert.AreEqual("Hello", result1, "Should return the first word");

        var result2 = _textActions.ExtractRegex(
            new TextDto { Text = "HELLO world" },
            new RegexInput { Regex = @"hello", Flags = new[] { "insensitive" } }
        );
        Console.WriteLine(result2);
        Assert.AreEqual("HELLO", result2, "Should match case-insensitively");

        var result3 = _textActions.ExtractRegex(
            new TextDto { Text = "My number is 123-456" },
            new RegexInput { Regex = @"(\d+)-(\d+)", Group = "2" }
        );
        Console.WriteLine(result3);
        Assert.AreEqual("456", result3, "Should return the second group");

        var result4 = _textActions.ExtractRegex(
            new TextDto { Text = "Line1\r\nStartLine2\r\nLine3  " },
            new RegexInput { Regex = @"^Start\w+", Flags = new[] { "multiline" } }
        );
        Console.WriteLine(result4);
        Assert.AreEqual("StartLine2", result4, "Should match the start of the second line");

        var result5 = _textActions.ExtractRegex(
            new TextDto { Text = "Line1\nLine2\nLine3" },
            new RegexInput { Regex = @".+", Flags = new[] { "singleline" } }
        );
        Console.WriteLine(result5);
        Assert.AreEqual("Line1\nLine2\nLine3", result5, "Should match across newlines");

        var result6 = _textActions.ExtractRegex(
            new TextDto { Text = "Hello World" },
            new RegexInput { Regex = @"\d+" }
        );
        Console.WriteLine(result6);
        Assert.AreEqual("", result6, "Should return empty string when no match is found");

        var result7 = _textActions.ExtractRegex(
            new TextDto { Text = "HELLO WORLD" },
            new RegexInput { Regex = @"hello\s+world", Flags = new[] { "insensitive", "extended" } }
        );
        Console.WriteLine(result7);
        Assert.AreEqual("HELLO WORLD", result7, "Should match case-insensitively with extended flag ignoring whitespace in regex");
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

    [TestMethod]
    public void ReplaceManyRegex_Works()
    {
        var textDto = new TextDto { Text = "Übermäßig süße Bären mögen Öl und Käse." };
        var input = new RegexReplaceMultipleInput
        {
            RegexPatterns = ["Ä", "ä", "Ö", "ö", "Ü", "ü", "ß"],
            Replacements = ["Ae", "ae", "Oe", "oe", "Ue", "ue", "ss"]
        };
        var result = _textActions.ReplaceManyRegex(textDto, input);
        var expected = "Uebermaessig suesse Baeren moegen Oel und Kaese.";
        Console.WriteLine(result);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ConvertToUpperCase_ReturnsUpperCaseText()
    {
        // Arrange
        var dto = new TextDto { Text = "hello, world!" };
        var actions = new Texts(InvocationContext);

        // Act
        var result = actions.ConvertToUpperCase(dto);

        // Assert
        Console.WriteLine(result);
        Assert.AreEqual(dto.Text.ToUpperInvariant(), result);
    }
}