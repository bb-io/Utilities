using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files;

public class ExtractTextFromDocumentRequest : RegexInput
{
    public FileReference File { get; set; }
}