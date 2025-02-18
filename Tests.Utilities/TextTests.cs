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
    public Task TrimText_TrimSizeLongerThanText_ReturnsEmptyString()
    {
        var text = "not a very long text";

        var result = _textActions.TrimText(new TextDto { Text = text},new TrimTextInput { CharactersFromEnd = 60000});

        Assert.IsTrue(result.Length == 0);
        return Task.CompletedTask;
    }
    
    [TestMethod]
    public Task SplitStringToArray_CommaSeparated_ReturnsCorrectArray()
    {
        var textDto = new TextDto { Text = "en,de,fr"};
        var expected = new List<string> { "en", "de", "fr" };

        var result = _textActions.SplitStringToArray(textDto, new() { Delimiter = ","});

        CollectionAssert.AreEqual(expected, result);
        return Task.CompletedTask;
    }

    [TestMethod]
    public Task SplitStringToArray_CustomDelimiter_ReturnsCorrectArray()
    {
        var textDto = new TextDto { Text = "apple | banana | cherry"};
        var expected = new List<string> { "apple", "banana", "cherry" };

        var result = _textActions.SplitStringToArray(textDto, new() { Delimiter = " | "});

        CollectionAssert.AreEqual(expected, result);
        return Task.CompletedTask;
    }

    [TestMethod]
    public Task SplitStringToArray_HandlesExtraSpaces_ReturnsTrimmedArray()
    {
        var textDto = new TextDto { Text = "  en , de ,  fr  "};
        var expected = new List<string> { "en", "de", "fr" };

        var result = _textActions.SplitStringToArray(textDto, new() { Delimiter = ","});

        CollectionAssert.AreEqual(expected, result);
        return Task.CompletedTask;
    }

    [TestMethod]
    [ExpectedException(typeof(PluginMisconfigurationException))]
    public Task SplitStringToArray_EmptyString_ThrowsException()
    {
        _textActions.SplitStringToArray(new(), new() { Delimiter = ","});
        return Task.CompletedTask;
    }
}