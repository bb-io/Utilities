using Apps.Utilities.DataSourceHandlers;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files;

public class ReplaceTextInDocumentRequest : RegexReplaceInput
{
    public FileReference File { get; set; } = new();

    [StaticDataSource(typeof(CultureSourceHandler))]
    public string? ExprimentalRegexField { get; set; }
}