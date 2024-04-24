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

    //[Action("Create/add many to array", Description = "Creates an array or uses the one provided as original. Adds content to array if \"ArayToBeAded\" is provided")]
    //public ArrayAddCreateResponse AddToArray([ActionParameter] ArrayAddCreateRequest input)
    //{
    //    List<string> myList = input.OriginalArray?.ToList() ?? new List<string>();
    //    if (input.ArrayToBeAdded != null)
    //    {
    //        myList.AddRange(input.ArrayToBeAdded.ToList());
    //    }
    //    return new ArrayAddCreateResponse {MyArray = myList };
    //}

    //[Action("Create/add single element to array", Description = "Creates an array or uses the one provided as original. Adds element to array if \"Item\" is provided")]
    //public ArrayAddCreateResponse AddSingleToArray([ActionParameter] ArrayAddCreateSingleRequest input)
    //{
    //    List<string> myList = input.Array?.ToList() ?? new List<string>();
    //    if (!String.IsNullOrEmpty(input.Item))
    //    {
    //        myList.Add(input.Item);
    //    }
    //    return new ArrayAddCreateResponse { MyArray = myList };
    //}

    [Action("Deduplicate Array", Description = "Returns only unique elements")]
    public ArrayAddCreateResponse DeduplicateArray([ActionParameter] IEnumerable<string> input)
    {
        return new ArrayAddCreateResponse { MyArray = input.Distinct() };
    }

    
}