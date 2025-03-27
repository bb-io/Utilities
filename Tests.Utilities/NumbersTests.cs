using Apps.Utilities.Actions;
using Apps.Utilities.Models.Numbers.Requests;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class NumbersTests : TestBase
{
    private Numbers _numberActions;

    [TestInitialize]
    public void Init()
    {
        var outputDirectory = Path.Combine(GetTestFolderPath(), "Output");
        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, true);
        }

        Directory.CreateDirectory(outputDirectory);

        _numberActions = new Numbers();
    }

    [TestMethod]
    public Task ConvertTextsToNumbers_ValidNumbers_ReturnsCorrectList()
    {
        var input = new List<string> { "1.5", "2", "3.14", "-4", "0" };
        var expected = new List<double> { 1.5, 2, 3.14, -4, 0 };

        var result = _numberActions.ConvertTextsToNumbers(new() { NumericStrings = input });

        CollectionAssert.AreEqual(expected, result.Numbers.ToList());
        return Task.CompletedTask;
    }

    [TestMethod]
    [ExpectedException(typeof(PluginMisconfigurationException))]
    public Task ConvertTextsToNumbers_InvalidNumber_ThrowsException()
    {
        var input = new List<string> { "1", "abc", "3" };

        _numberActions.ConvertTextsToNumbers(new() { NumericStrings = input });
        return Task.CompletedTask;
    }

    [TestMethod]
    [ExpectedException(typeof(PluginMisconfigurationException))]
    public Task ConvertTextsToNumbers_EmptyString_ThrowsException()
    {
        var input = new List<string> { " " };

        _numberActions.ConvertTextsToNumbers(new() { NumericStrings = input });
        return Task.CompletedTask;
    }

    [TestMethod]
    public Task ConvertTextsToNumbers_MixedValidAndInvalid_ThrowsException()
    {
        var numericStrings = new List<string> { "42", "invalid", "3.5" };

        Assert.ThrowsException<PluginMisconfigurationException>(() =>
            _numberActions.ConvertTextsToNumbers(new() { NumericStrings = numericStrings }));

        return Task.CompletedTask;
    }

    [TestMethod]
    public Task ConvertTextsToNumbers_EmptyList_ReturnsEmptyList()
    {
        var input = new List<string>();

        var result = _numberActions.ConvertTextsToNumbers(new() { NumericStrings = input });

        Assert.AreEqual(0, result.Numbers.Count());
        return Task.CompletedTask;
    }
}