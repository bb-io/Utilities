using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Json
{
    public class GetJsonPropertyInput
    {
        [Display("Property path")]
        public string PropertyPath { get; set; }

        [Display("JSON file")]
        public FileReference? File { get; set; }

        [Display("JSON string")]
        public string? JsonString { get; set; }
    }
}
