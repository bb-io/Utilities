using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers;

public class XliffReplacementScopeDataSourceHandler : IStaticDataSourceItemHandler
{
    public const string Target = "target";
    public const string Source = "source";
    public const string Both = "both";

    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(Target, "Target only (default)"),
        new(Source, "Source only"),
        new(Both, "Both target and source"),
    ];
}
