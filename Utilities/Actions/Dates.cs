using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.Utilities.Models.Dates;

namespace Apps.Utilities.Actions
{
    [ActionList]
    public class Dates : BaseInvocable
    {
        public Dates(InvocationContext context) : base(context) { }

        [Action("Generate date", Description = "Generate a date relative to the moment this action is called.")]
        public DateResponse GenerateDate([ActionParameter] GenerateDateRequest input)
        {
            return new DateResponse { Date = DateTime.Now.AddDays(input.AddDays ?? 0).AddHours(input.AddHours ?? 0).AddMinutes(input.AddMinutes ?? 0) };
        }

        [Action("Format date", Description = "Format a date to text according to pre-defined formatting rules and culture")]
        public FormattedDateResponse FormatDate([ActionParameter] FormatDateRequest input )
        {
            return new FormattedDateResponse { FormattedDate = input.Date.ToString(input.Format, input.Culture != null ? new CultureInfo(input.Culture) : CultureInfo.InvariantCulture) };
        }
    }
}
