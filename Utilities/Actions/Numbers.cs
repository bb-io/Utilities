using Apps.Utilities.Models.Shared;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        public int ConvertTextToNumber([ActionParameter] string Text)
        {
            return int.Parse(Text);

        }
    }
}
