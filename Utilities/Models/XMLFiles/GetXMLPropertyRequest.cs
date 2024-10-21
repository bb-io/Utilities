using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles
{
    public class GetXMLPropertyRequest
    {
        public FileReference File { get; set; }

        public string Property { get; set; }

        public string? Attribute { get; set; }
    }
}
