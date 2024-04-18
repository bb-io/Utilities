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
    public ArrayContainsResponse GenerateDate([ActionParameter] ArrayContainsRequest input)
    {
        return new()
        {
            Contains = input.Array.Contains(input.Entry)
        };
    }

    [Action("Create/add many to array", Description = "Check if array contains a ceratin entry")]
    public ArrayAddCreateResponse AddToArray([ActionParameter] ArrayAddCreateRequest input)
    {
        var myList = new List<string>();
        if (input.Array != null)
        {
            myList.AddRange(input.Array);
        }
        return new ArrayAddCreateResponse {MyGroup = myList };
    }

    [Action("Create/add single element to array", Description = "Check if array contains a ceratin entry")]
    public ArrayAddCreateResponse AddSingleToArray([ActionParameter] ArrayAddCreateSingleRequest input)
    {
        var myList = new List<string>();
        if (String.IsNullOrEmpty(input.Item))
        {
            myList.Add(input.Item);
        }
        return new ArrayAddCreateResponse { MyGroup = myList };
    }

    [Action("Deduplicate Array", Description = "Return only unique elements")]
    public IEnumerable<string> DeduplicateArray([ActionParameter] IEnumerable<string> input)
    {
        return input.Distinct();
    }
}