using Blackbird.Applications.Sdk.Common.Dictionaries;
using System.Globalization;

namespace Apps.Utilities.DataSourceHandlers;

public class CultureSourceHandler : IStaticDataSourceHandler
{
    public Dictionary<string, string> GetData()
    {
        var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        return cultures.ToDictionary(x => x.Name, x => x.EnglishName);
    }
}