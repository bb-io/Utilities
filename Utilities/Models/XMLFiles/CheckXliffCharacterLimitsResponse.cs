using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.XMLFiles;

public class CheckXliffCharacterLimitsResponse
{
    [Display("Total units")]
    public int TotalUnits { get; set; }

    [Display("Units matching state filter")]
    public int UnitsMatchingStateFilter { get; set; }

    [Display("Total units with limits")]
    public int TotalUnitsWithLimits { get; set; }
    
    [Display("Units over limits")]
    public int UnitsOverLimits { get; set; }

    public List<XliffCharacterLimitUnit> Units { get; set; } = [];
}

public class XliffCharacterLimitUnit
{
    [Display("Unit ID")]
    public string UnitId { get; set; } = string.Empty;

    [Display("Maximum length")]
    public int MaximumLength { get; set; }
    
    [Display("Current length")]
    public int CurrentLength { get; set; }
}
