using Apps.Utilities.Models.Shared;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Text.RegularExpressions;

namespace Apps.Utilities.Actions;

[ActionList]
public class Texts : BaseInvocable
{
    public Texts(InvocationContext context) : base(context) { }

    [Action("Sanitize text", Description = "Remove any defined characters from a text.")]
    public TextDto SanitizeText([ActionParameter] TextDto text, [ActionParameter] SanitizeRequest input)
    {
        var newText = text.Text;
        foreach (string filteredCharacter in input.FilterCharacters)
        {
            newText = newText.Replace(filteredCharacter, string.Empty);
        }
        return new TextDto { Text = newText };
    }

    [Action("Count characters in text", Description = "Returns number of chracters in text.")]
    public int CountCharsInText([ActionParameter] TextDto input)
    {
        return input.Text.Length;
    }

    [Action("Count words in text", Description = "Returns number of words in text.")]
    public int CountWordsInText([ActionParameter] TextDto input)
    {
        char[] punctuationCharacters = input.Text.Where(char.IsPunctuation).Distinct().ToArray();
        var words = input.Text.Split().Select(x => x.Trim(punctuationCharacters));
        return words.Where(x => !string.IsNullOrWhiteSpace(x)).Count();
    }

    [Action("Extract using Regex", Description = "Returns first match from text using input Regex")]
    public string ExtractRegex([ActionParameter] TextDto input, [ActionParameter] RegexInput regex)
    {
        return Regex.Match(input.Text, Regex.Escape(regex.Regex)).Value;
    }

    [Action("Extract many using Regex", Description = "Returns all matches from text using input Regex")]
    public List<string> ExtractManyRegex([ActionParameter] TextDto input, [ActionParameter] RegexInput regex)
    {
        return Regex.Matches(input.Text, Regex.Escape(regex.Regex))
            .OfType<Match>()
            .Select(m => m.Value)
            .ToList();
    }


}