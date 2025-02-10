using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Json
{
    public class ChangeJsonPropertyInput
    {
        [Display("JSON string")]
        public string JsonString { get; set; }

        [Display("Property path")]
        public string PropertyPath { get; set; }

        [Display("New JSON value")]
        public object NewValue { get; set; }

        [Display("JSON file")]
        public FileReference File { get; set; }
    }
}
