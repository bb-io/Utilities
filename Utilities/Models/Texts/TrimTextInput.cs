using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts;

public class TrimTextInput
{
    [Display("Characters from end")]
    public int? CharactersFromEnd { get; set; }   
    
    [Display("Characters from beginning")]
    public int? CharactersFromBeginning { get; set; }
    
    [Display("Trim spaces")]
    public bool? TrimSpaces { get; set; }
}