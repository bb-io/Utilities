using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Models.Texts
{
    public class OccurencesDto
    {
        [Display("Options", Description = "Will return multiple texts of all these options if they occur in the text.")]
        public IEnumerable<string> Options { get; set; }
    }
}
