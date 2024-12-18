﻿using Apps.Utilities.Models.Shared;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Text.RegularExpressions;
using BleuNet;

namespace Apps.Utilities.Actions;

[ActionList]
public class Texts : BaseInvocable
{
    public Texts(InvocationContext context) : base(context)
    {
    }

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
        if (String.IsNullOrEmpty(regex.Group))
        {
            return Regex.Match(input.Text, Regex.Unescape(regex.Regex)).Value;
        }
        else
        {
            return Regex.Match(input.Text, Regex.Unescape(regex.Regex)).Groups[regex.Group].Value;
        }
    }

    [Action("Extract many using Regex", Description = "Returns all matches from text using input Regex")]
    public List<string> ExtractManyRegex([ActionParameter] TextDto input, [ActionParameter] RegexManyInput regex)
    {
        return Regex.Matches(input.Text, Regex.Unescape(regex.Regex))
            .OfType<Match>()
            .Select(m => m.Value)
            .ToList();
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
        return Regex.Replace(input.Text, Regex.Unescape(regex.Regex), Regex.Unescape(regex.Replace));
    }

    [Action("Trim text", Description = "Trim specified text")]
    public string TrimText([ActionParameter] TextDto text, [ActionParameter] TrimTextInput input)
    {
        var result = text.Text;

        if (input.CharactersFromBeginning is not null)
            result = result.Remove(0, input.CharactersFromBeginning.Value);

        if (input.CharactersFromEnd is not null)
            result = result.Remove(result.Length - input.CharactersFromEnd.Value);

        if (input.TrimSpaces is true)
            result = result.Trim();

        return result;
    }
}