using Apps.Utilities.DataSourceHandlers;
using Apps.Utilities.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Exceptions;
using System.Text.RegularExpressions;

namespace Apps.Utilities.Models.Texts;

public class RegexInput
{
    [Display("Regular Expression")]
    public string Regex { get; set; }

    [Display("Replace pattern")]
    public string? Replace { get; set; }

    [Display("Group Number")]
    public string? Group { get; set; }

    [Display("Replace value from")]
    public IEnumerable<string>? From { get; set; }

    [Display("Replace value to")]
    public IEnumerable<string>? To { get; set; }

    [Display("Regex Flags")]
    [StaticDataSource(typeof(RegexFlagsSourceHandler))]
    public IEnumerable<string>? Flags { get; set; }

    public RegexInput Validate()
    {
        if (Regex == null || string.IsNullOrWhiteSpace(Regex))
            throw new PluginMisconfigurationException("Regex pattern cannot be null or empty");

        Regex reg;
        try
        {
            reg = new Regex(Regex, RegexOptionsUtillity.GetRegexOptions(Flags));
        }
        catch (RegexParseException)
        {
            throw new PluginMisconfigurationException("Invalid regex pattern");
        }

        return this;
    }
}
