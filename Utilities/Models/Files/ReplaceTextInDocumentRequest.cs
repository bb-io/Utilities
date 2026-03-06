using Apps.Utilities.DataSourceHandlers;
using Apps.Utilities.Models.Texts;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files;

public class ReplaceTextInDocumentRequest : RegexReplaceInput
{
    public FileReference File { get; set; } = new();

    [Display("Experimental regex replace pattern input")]
    [StaticDataSource(typeof(CultureSourceHandler))]
    public string? ExprimentalRegexField { get; set; }
}