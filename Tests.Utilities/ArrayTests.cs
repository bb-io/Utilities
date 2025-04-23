using Apps.Utilities.Actions;
using Apps.Utilities.Models.Arrays.Request;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Tests.Utilities.Base;

namespace Tests.Utilities;

[TestClass]
public class ArrayTests : TestBase
{

    [TestMethod]
    public void Array_intersect_works()
    {
        var array1 = new List<string> { "one", "two", "three" };
        var array2 = new List<string> { "two", "three", "four" };

        var actions = new Arrays(InvocationContext);
        var result = actions.ArrayIntersect(new ArrayIntersectionRequest { FirstArray = array1, SecondArray = array2 });

        Assert.IsTrue(result.Array.Contains("two") && result.Array.Contains("three") && result.Array.Count() == 2);
    }    
}