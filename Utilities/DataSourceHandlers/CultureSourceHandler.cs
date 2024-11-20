using Blackbird.Applications.Sdk.Common.Dictionaries;
using System.Globalization;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers;

public class CultureSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        return cultures.Select(x => new DataSourceItem(x.Name, x.EnglishName));
    }
}