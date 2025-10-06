using Apps.Utilities.Models.Arrays.Request;
using Apps.Utilities.Models.Arrays.Response;
using Apps.Utilities.Models.Texts;
using Apps.Utilities.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using System.Text.RegularExpressions;

namespace Apps.Utilities.Actions;

[ActionList("Arrays")]
public class Arrays(InvocationContext invocationContext) : BaseInvocable(invocationContext)
{
    [Action("Array contains", Description = "Check if array contains a ceratin entry")]
    public ArrayContainsResponse ArrayContains([ActionParameter] ArrayContainsRequest input)
    {
        return new()
        {
            Contains = input.Array.Contains(input.Entry)
        };
    }

    [Action("Array count", Description = "Counts the number of elements in an array")]
    public double ArrayCount([ActionParameter] ArrayCountRequest input)
    {
        return input.Array.Count();
    }

    [Action("Extract matches from array using Regex", Description = "From an array of strings, return extracted matches for elements that satisfy the regex. If 'Group' is set, returns that group's value; otherwise full match.")]
    public async Task<ExtractArrayResponse> ExtractArrayUsingRegex([ActionParameter] TextsDto input, [ActionParameter] RegexInput regex)
    {
        if (input == null || input.Texts == null)
            throw new PluginMisconfigurationException("Input array cannot be null.");
        if (regex == null || string.IsNullOrWhiteSpace(regex.Regex))
            throw new PluginMisconfigurationException("Regex pattern cannot be null or empty.");

        var options = RegexOptionsUtillity.GetRegexOptions(regex.Flags);
        var r = new Regex(regex.Regex, options);

        var results = new List<string>();
        foreach (var s in input.Texts)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;

            var m = r.Match(s);
            if (!m.Success) continue;

            if (string.IsNullOrWhiteSpace(regex.Group))
            {
                results.Add(m.Value);
            }
            else
            {
                if (!m.Groups.ContainsKey(regex.Group))
                    throw new PluginMisconfigurationException($"Group '{regex.Group}' not found in the regex pattern");
                results.Add(m.Groups[regex.Group].Value);
            }
        }
        return new ExtractArrayResponse { Response = results };
    }


    [Action("Deduplicate array", Description = "Returns only unique elements")]
    public ArrayResponse DeduplicateArray([ActionParameter] ArrayCountRequest input)
    {
        return new ArrayResponse { Array = input.Array.Distinct() };
    }

    [Action("Remove entries from array", Description = "Returns the array without the specified entries")]
    public ArrayResponse ArrayRemove([ActionParameter] ArrayRemoveRequest input)
    {
        return new()
        {
            Array = input.Array.Except(input.removeEntries).ToArray()
        };
    }

    [Action("Get first entry from array", Description = "Returns the first element in the array")]
    public string FirstArray([ActionParameter] ArrayCountRequest input)
    {
        return input.Array.FirstOrDefault();
    }

    [Action("Get last entry from array", Description = "Returns the last element in the array")]
    public string LastArray([ActionParameter] ArrayCountRequest input)
    {
        return input.Array.LastOrDefault();
    }

    [Action("Get entry by position", Description = "Returns the element in the specified position within the array")]
    public string GetEntryInPosition([ActionParameter] ArrayCountRequest input,
        [ActionParameter] int Position)
    {
        if (input.Array == null || !input.Array.Any() || Position <= 0 || input.Array.Count() < Position)
        {
            throw new PluginMisconfigurationException("Position is out of bounds or invalid");
        }
        return input.Array.ToList()[Position - 1];
    }

    [Action("Retain specified entries in array", Description = "Returns the array without the entries that were not present in the provided control array")]
    public ArrayResponse ArrayFilter([ActionParameter] ArrayFilterRequest input)
    {
        return new()
        {
            Array = input.Array.Where(x => input.Control.Contains(x))
        };
    }

    [Action("Array intersection", Description = "Returns the intersection of two input arrays (returns the elements contained in both arrays)")]
    public ArrayResponse ArrayIntersect([ActionParameter] ArrayIntersectionRequest input)
    {
        return new()
        {
            Array = input.FirstArray.Intersect(input.SecondArray)
        };
    }
}

