using System.Globalization;
using Apps.Utilities.Models.Numbers.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Utilities.Actions
{
    [ActionList]
    public class Numbers
    {
        [Action("Generate range", Description = "Generate a range by providing start and end numbers")]
        public List<int> CreateRange([ActionParameter] int Start, [ActionParameter] int End)
        {
            var myList = new List<int>();
            if (Start < End)
            {
                for (var i = Start; i <= End; i++)
                {
                    myList.Add(i);
                }
                return myList;
            }
            else
            {
                for (var i = Start; i >= End; i--)
                {
                    myList.Add(i);
                }
                return myList;
            }
        }

        [Action("Convert text to number", Description = "Change the type of data")]
        public ConvertTextToNumberResponse ConvertTextToNumber([ActionParameter] string Text)
        {
            return new ConvertTextToNumberResponse { Number = double.Parse(Text) };
        }
        
        [Action("Convert texts to numbers", Description = "Change the type of data")]
        public ConvertTextsToNumbersResponse ConvertTextToNumber([ActionParameter] IEnumerable<string> Texts)
        {
            return new ConvertTextsToNumbersResponse { Numbers = Texts.Select(text =>
            {
                if (double.TryParse(text, CultureInfo.InvariantCulture, out var number))
                {
                    return number;
                }
                
                throw new PluginMisconfigurationException($"Couldn't parse given text ({text}) to number. Please verify you sent valid numbers to this action");
            }).ToList()};
        }
    }
}
