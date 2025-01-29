using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts
{
    public class ConcatenateStringsInput
    {
        [Display("Strings")]
        public IEnumerable<string> Strings { get; set; }

        [Display("Delimiter")]
        public string Delimiter { get; set; }
    }
}

