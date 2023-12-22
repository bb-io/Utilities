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
}