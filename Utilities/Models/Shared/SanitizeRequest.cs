using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Models.Shared
{
    public class SanitizeRequest
    {
        [Display("Characters to remove")]
        public IEnumerable<string> FilterCharacters { get; set; }
    }
}
