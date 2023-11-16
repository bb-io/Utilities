using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Models.Files
{
    public class RenameRequest
    {
        [Display("New name")]
        public string Name { get; set; }
    }
}
