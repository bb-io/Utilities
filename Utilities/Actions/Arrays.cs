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
}