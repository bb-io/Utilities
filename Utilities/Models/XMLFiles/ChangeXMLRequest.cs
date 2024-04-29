using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles
{
    public class ChangeXMLRequest
    {
        public FileReference File { get; set; }

        public string Property { get; set; }

        public string Value { get; set; }
    }
}
