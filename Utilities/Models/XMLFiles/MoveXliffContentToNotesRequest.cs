using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class MoveXliffContentToNotesRequest
{
    [Display("XLIFF file")]
    public FileReference File { get; set; } = new();

    [Display("XPath")]
    public string XPath { get; set; } = "//ns:target";

    [Display("Attribute to move")]
    public string? Attribute { get; set; }

    [Display("Namespace URI for XPath")]
    public string? Namespace { get; set; }

    [Display("Remove source after moving")]
    public bool? RemoveSource { get; set; }

    [Display("Include source name in note")]
    public bool? IncludeSourceNameInNote { get; set; }
}
