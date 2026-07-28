using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers;

public class XliffOutputFormatDataSourceHandler : IStaticDataSourceItemHandler
{
    public const string Original = "original";
    public const string Xliff12 = "xliff_1_2";
    public const string Xliff22 = "xliff_2_2";

    public IEnumerable<DataSourceItem> GetData() =>
    [
        new(Original, "Original file (default)"),
        new(Xliff12, "XLIFF 1.2"),
        new(Xliff22, "XLIFF 2.2"),
    ];
}
