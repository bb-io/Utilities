using Blackbird.Applications.Sdk.Common.Dynamic;
using System.Globalization;

namespace Apps.Utilities.DataSourceHandlers;

public class CultureSourceHandler : IDataSourceHandler
{
    public Dictionary<string, string> GetData(DataSourceContext context)
    {
        var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        return cultures
            // Applying user search query to the response
            .Where(x => context.SearchString == null ||
                        x.EnglishName.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Name, x => x.EnglishName);
    }
}