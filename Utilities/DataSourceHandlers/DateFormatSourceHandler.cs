using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers;

public class DateFormatSourceHandler : IStaticDataSourceHandler
{
    private Dictionary<string, string> EnumValues => new()
    {
        {"d", "Short date"},
        {"D", "Long date"},
        {"f", "Full date/time (short time)"},
        {"F", "Full date/time (long time)" },
        {"g", "General date/time (short time)"},
        {"G", "General date/time (long time)" },
        {"M", "Month/day" },
        {"R", "RFC1123" },
        {"t", "Short time" },
        {"T", "Long time" },
        {"U", "Universal full date/time" },
        {"Y", "Year month" }
    };

    public Dictionary<string, string> GetData()
    {
        return EnumValues;
    }
}