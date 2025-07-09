using System.Globalization;
using Apps.Utilities.Models.Numbers.Requests;
using Apps.Utilities.Models.Numbers.Response;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;

namespace Apps.Utilities.Actions
{
    [ActionList("Numbers")]
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
            if (string.IsNullOrWhiteSpace(Text))
                throw new PluginMisconfigurationException("Text parameter is null or empty. Please check your inputs and try again");

            var culture = CultureInfo.CurrentCulture;

            if (double.TryParse(Text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out double result))
            {
                return new ConvertTextToNumberResponse { Number = double.Parse(Text) };
            }
            else
            {
                throw new PluginMisconfigurationException($"Couldn't parse given text ({Text}) to number. Please verify you sent a valid number to this action");
            }
        }

        [Action("Convert text to numbers", Description = "Converts a list of numeric strings into a list of numbers. Throws an exception if any value is not a valid number")]
        public ConvertTextsToNumbersResponse ConvertTextsToNumbers([ActionParameter] ConvertTextsToNumbersRequest request)
        {
            if (request?.NumericStrings == null || !request.NumericStrings.Any())
                throw new PluginMisconfigurationException("NumericStrings parameter is null or empty. Please check your inputs and try again");

            var culture = CultureInfo.CurrentCulture;
            var numbers = new List<double>();

            foreach (var text in request.NumericStrings)
            {
                if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var number))
                {
                    numbers.Add(number);
                }
                else
                {
                    throw new PluginMisconfigurationException($"Couldn't parse given text ('{text}') to number. Please verify you sent valid numbers to this action");
                }
            }

            return new ConvertTextsToNumbersResponse { Numbers = numbers };
        }
    }
}
