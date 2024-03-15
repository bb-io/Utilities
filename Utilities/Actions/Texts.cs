using Apps.Utilities.Models.Shared;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

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


    
}