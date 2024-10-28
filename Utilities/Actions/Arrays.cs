using Apps.Utilities.Models.Arrays.Request;
using Apps.Utilities.Models.Arrays.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using DocumentFormat.OpenXml.Presentation;

namespace Apps.Utilities.Actions;

[ActionList]
public class Arrays : BaseInvocable
{
    public Arrays(InvocationContext invocationContext) : base(invocationContext)
    {
    }

    [Action("Array contains", Description = "Check if array contains a ceratin entry")]
    public ArrayContainsResponse ArrayContains([ActionParameter] ArrayContainsRequest input)
    {
        return new()
        {
            Contains = input.Array.Contains(input.Entry)
        };
    }

    [Action("Array count", Description = "Counts the number of elements in an array")]
    public int ArrayCount([ActionParameter] ArrayCountRequest input)
    {
        return input.Array.Count();
    }


    [Action("Deduplicate array", Description = "Returns only unique elements")]
    public ArrayResponse DeduplicateArray([ActionParameter] ArrayCountRequest input)
    {
        return new ArrayResponse { Array = input.Array.Distinct() };
    }

    [Action("Remove entry from array", Description = "Returns the array without the specified entry")]
    public ArrayResponse ArrayRemove([ActionParameter] ArrayContainsRequest input)
    {
        return new()
        {
            Array = input.Array.Where(x => x != input.Entry).ToArray()
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
}

