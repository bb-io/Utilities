using Apps.Utilities.Models.Arrays.Request;
using Apps.Utilities.Models.Arrays.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

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
    public ArrayResponse DeduplicateArray([ActionParameter] IEnumerable<string> input)
    {
        return new ArrayResponse { Array = input.Distinct() };
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
    public string FirstArray([ActionParameter] IEnumerable<string> input)
    {
        return input.ToList().FirstOrDefault();
    }

}