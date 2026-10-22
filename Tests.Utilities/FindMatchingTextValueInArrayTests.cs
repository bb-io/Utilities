using Apps.Utilities.Actions;
using Apps.Utilities.Models.Arrays.Request;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Tests.Utilities;

[TestClass]
public class FindMatchingTextValueInArrayTests
{
    private readonly Arrays _actions = new(new InvocationContext());

    [TestMethod]
    public void ReturnsEntireFirstElementContainingPartialMatch()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = ["prefix123", "123suffix", "123\n", "123", "456"],
            Regex = @"\d+"
        });

        Assert.AreEqual("prefix123", result.MatchingText);
    }

    [TestMethod]
    public void SupportsPartialMatchesWithAlternatives()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = ["prefixab", "ab", "a"],
            Regex = "a|ab"
        });

        Assert.AreEqual("prefixab", result.MatchingText);
    }

    [TestMethod]
    public void HonorsExplicitWholeElementAnchors()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = ["prefix123", "123suffix", "123\n", "123", "456"],
            Regex = @"\A(?:\d+)\z"
        });

        Assert.AreEqual("123", result.MatchingText);
    }

    [TestMethod]
    public void CaseSensitiveByDefault()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = ["HELLO", "hello"],
            Regex = "hello"
        });

        Assert.AreEqual("hello", result.MatchingText);
    }

    [TestMethod]
    public void AppliesCombinedRegexFlagsAndPreservesOriginalText()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = ["HELLO\nWORLD", "hello world"],
            Regex = "hello . world",
            Flags = ["insensitive", "singleline", "extended"]
        });

        Assert.AreEqual("HELLO\nWORLD", result.MatchingText);
    }

    [TestMethod]
    public void MultilineFlagAllowsMatchingAnIndividualLine()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = ["prefix\nhello\nsuffix", "hello\n", "hello"],
            Regex = "^hello$",
            Flags = ["multiline"]
        });

        Assert.AreEqual("prefix\nhello\nsuffix", result.MatchingText);
    }

    [DataTestMethod]
    [DataRow("right to left")]
    [DataRow("non-capturing")]
    [DataRow("no backtracking")]
    public void SupportsAdditionalRegexFlags(string flag)
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = ["prefixab", "ab", "a"],
            Regex = "(a|ab)",
            Flags = [flag]
        });

        Assert.AreEqual("prefixab", result.MatchingText);
    }

    [TestMethod]
    public void SupportsTrailingCommentsWithExtendedFlag()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = ["prefix123", "123"],
            Regex = @"\d+ # digits",
            Flags = ["extended"]
        });

        Assert.AreEqual("prefix123", result.MatchingText);
    }

    [TestMethod]
    public void ReturnsEmptyTextWhenNoElementMatches()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = ["abc", "def"],
            Regex = @"\d+"
        });

        Assert.AreEqual(string.Empty, result.MatchingText);
    }

    [TestMethod]
    public void ReturnsEmptyTextForEmptyArray()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = [],
            Regex = ".*"
        });

        Assert.AreEqual(string.Empty, result.MatchingText);
    }

    [TestMethod]
    public void SkipsNullElementsAndAllowsWhitespaceMatches()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = [null!, "", " ", "  "],
            Regex = @"\s+"
        });

        Assert.AreEqual(" ", result.MatchingText);
    }

    [TestMethod]
    public void EmptyStringCanBeTheFirstMatch()
    {
        var result = _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
        {
            Array = ["", "abc"],
            Regex = ".*"
        });

        Assert.AreEqual(string.Empty, result.MatchingText);
    }

    [TestMethod]
    public void RejectsNullInputOrArray()
    {
        Assert.ThrowsException<PluginMisconfigurationException>(
            () => _actions.FindMatchingTextValueInArray(null!));
        Assert.ThrowsException<PluginMisconfigurationException>(
            () => _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
            {
                Array = null!,
                Regex = ".*"
            }));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("[")]
    [DataRow(")|(?:abc")]
    public void RejectsMissingOrInvalidRegex(string? pattern)
    {
        Assert.ThrowsException<PluginMisconfigurationException>(
            () => _actions.FindMatchingTextValueInArray(new ArrayFindMatchingTextValueRequest
            {
                Array = ["abc"],
                Regex = pattern!
            }));
    }
}
