using Apps.Utilities.Models.Shared;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Text.RegularExpressions;
using BleuNet;
using Blackbird.Applications.Sdk.Common.Exceptions;
using DocumentFormat.OpenXml.ExtendedProperties;
using System.Text;

namespace Apps.Utilities.Actions;

[ActionList]
public class Texts(InvocationContext context) : BaseInvocable(context)
{
    [Action("Calculate BLEU score",Description = "Evaluation of the quality of text which has been machine-translated from one natural language to another")]
    public BleuScore CalculateBleuScore(
        [ActionParameter][Display("Reference text", Description = "Reference text (human translation)")] string referenceText, 
        [ActionParameter][Display("Translated text", Description = "Translated part (machine translation)")] string translatedText)
    {
        if (string.IsNullOrWhiteSpace(referenceText) || string.IsNullOrWhiteSpace(translatedText))
        {
            throw new ArgumentException("Reference text and translated text cannot be null or empty.");
        }

        referenceText = referenceText.ToLower().Trim();
        translatedText = translatedText.ToLower().Trim();

        if (referenceText == translatedText)
        {
            return new BleuScore
            {
                Score = 1
            };
        }

        var referenceSentenceTokens = new string[][] {Utility.Tokenize( referenceText) };
        var translatedSentenceTokens = new string[][] { Utility.Tokenize(translatedText) };

        double score = Metrics.CorpusBleu(referenceSentenceTokens, translatedSentenceTokens);
        return new BleuScore
        {
            Score = score
        };
    }


    [Action("Sanitize text", Description = "Remove any defined characters from a text.")]
    public TextDto SanitizeText([ActionParameter] TextDto text, [ActionParameter] SanitizeRequest input)
    {
        var newText = text.Text;

        var filteredCharacters = input.FilterCharacters
        .Select(c => c.TrimEnd(' '))
        .Select(c => Regex.Escape(c))
        .ToList();

        foreach (string filteredCharacter in filteredCharacters)
        {
            newText = Regex.Replace(newText, filteredCharacter, string.Empty);
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

    [Action("Count words in texts", Description = "Returns number of words in text from array.")]
    public int CountWordsInTextFromArray([ActionParameter] TextsDto input)
    {
        int totalWords = 0;

        foreach (var text in input.Texts)
        {
            if (string.IsNullOrWhiteSpace(text))
                continue;

            char[] punctuationCharacters = text.Where(char.IsPunctuation).Distinct().ToArray();
            var words = text.Split().Select(x => x.Trim(punctuationCharacters));
            totalWords += words.Count(x => !string.IsNullOrWhiteSpace(x));
        }

        return totalWords;
    }

    [Action("Extract using Regex", Description = "Returns first match from text using input Regex")]
    public string ExtractRegex([ActionParameter] TextDto input, [ActionParameter] RegexInput regex)
    {
        if (String.IsNullOrEmpty(regex.Group))
        {
            return Regex.Match(input.Text, regex.Regex).Value;
        }
        else
        {
            return Regex.Match(input.Text, regex.Regex).Groups[regex.Group].Value;
        }
    }

    [Action("Extract many using Regex", Description = "Returns all matches from text using input Regex")]
    public List<string> ExtractManyRegex([ActionParameter] TextDto input, [ActionParameter] RegexManyInput regex)
    {
        if (input == null || string.IsNullOrEmpty(input.Text))
            throw new PluginMisconfigurationException("Input text cannot be null or empty.");

        if (regex == null || string.IsNullOrEmpty(regex.Regex))
            throw new PluginMisconfigurationException("Regular expression cannot be null or empty.");
        try
        {
            return Regex.Matches(input.Text, regex.Regex)
                .OfType<Match>()
                .Select(m => m.Value)
                .ToList();
        }
        catch (ArgumentException ex)
        {
            throw new PluginMisconfigurationException("The provided regular expression is invalid.", ex);
        }
        catch (Exception ex)
        {
            throw new PluginApplicationException("Error:", ex);
        }
    }

    [Action("Extract occurences from text", Description = "Returns all matches from text of a predefined list of possible options")]
    public List<string> ExtractOccurences([ActionParameter] TextDto input, [ActionParameter] OccurencesDto occurences)
    {
        var res = new List<string>();
        foreach (var c in occurences.Options)
        {
            if (input.Text.Contains(c)) res.Add(c);
        }
        return res;
    }

    [Action("Extract first occurence from text", Description = "Returns the first option that matches in a text of a predefined list of possible options")]
    public string? ExtractFirstOccurence([ActionParameter] TextDto input, [ActionParameter] OccurencesDto occurences)
    {
        foreach (var c in occurences.Options)
        {
            if (input.Text.Contains(c)) return c;
            
        }
        return null;
    }

    [Action("Replace using Regex", Description = "Use Regular Expressions to search and replace within text")]
    public string ReplaceRegex([ActionParameter] TextDto input, [ActionParameter] RegexReplaceInput regex)
    {
        if (input == null || input.Text == null)
            throw new PluginMisconfigurationException("Input text can not be null. Please check your input and try again");

        if (string.IsNullOrEmpty(regex.Regex))
            return input.Text;

        try
        {
            var r = new System.Text.RegularExpressions.Regex(regex.Regex);
            return r.Replace(input.Text, regex.Replace);
        }
        catch (ArgumentException ex)
        {
            throw new PluginApplicationException($"Wrong input format “{regex.Regex}” orstring to replace “{regex.Replace}”: {ex.Message}");
        }
    }

    [Action("Trim text", Description = "Trim specified text")]
    public string TrimText([ActionParameter] TextDto text, [ActionParameter] TrimTextInput input)
    {
        var result = text.Text;
            if (input.CharactersFromBeginning is not null)
            {
                if ( input.CharactersFromBeginning > result.Length)
                {
                    input.CharactersFromBeginning = result.Length;
                }
                 result = result.Remove(0, input.CharactersFromBeginning.Value);
            }

            if (input.CharactersFromEnd is not null)
            {
                if (input.CharactersFromEnd > result.Length)
                {
                    input.CharactersFromEnd = result.Length;
                }
                result = result.Remove(result.Length - input.CharactersFromEnd.Value);
            }

        if (input.TrimSpaces is true)
            result = result.Trim();

        return result;
    }


    [Action("Concatenate Strings", Description = "Concatenate Strings")]
    public string ConcatenateStrings([ActionParameter] ConcatenateStringsInput input)
    {
        if (input.Strings == null || !input.Strings.Any())
            throw new PluginMisconfigurationException("Strings list cannot be null or empty.");

        if (input.Delimiter == null)
            input.Delimiter = ",";

        return string.Join(input.Delimiter, input.Strings);
    }
    
    [Action("Split string into array", Description = "Splits a string into an array using the specified delimiter.")]
    public List<string> SplitStringToArray([ActionParameter] TextDto textDto, 
        [ActionParameter] DelimiterRequest delimiterRequest)
    {
        if (string.IsNullOrEmpty(textDto.Text))
        {
            throw new PluginMisconfigurationException("Input text cannot be null or empty.");
        }

        if (string.IsNullOrEmpty(delimiterRequest.Delimiter))
        {
            throw new PluginMisconfigurationException("Delimiter cannot be null or empty.");
        }
    
        return textDto.Text.Split([delimiterRequest.Delimiter], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
    }

    [Action("Generate random text", Description = "Generates a random text with definable length and characters used.")]
    public string GenerateRandomText(
        [ActionParameter][Display("Length", Description = "Length of the text. Default is 10 characters.")] int? length,
        [ActionParameter][Display("Characters", Description = "Characters used. Default is A-Z, a-z and 0-9")] string? characterSet
        )
    {
        length = length ?? 10;
        characterSet = characterSet ?? "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        StringBuilder result = new StringBuilder((int)length);
        Random random = new Random();

        for (int i = 0; i < length; i++)
        {
            int index = random.Next(characterSet.Length);
            result.Append(characterSet[index]);
        }

        return result.ToString();
    }
}