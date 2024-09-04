using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files;

public class ReplaceTextInDocumentResponse
{
    public FileReference File { get; set; } = new();
}