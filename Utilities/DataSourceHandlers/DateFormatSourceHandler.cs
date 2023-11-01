using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.DataSourceHandlers
{
    public class DateFormatSourceHandler : IDataSourceHandler
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

        public Dictionary<string, string> GetData(DataSourceContext context)
        {
            return EnumValues
                // Applying user search query to the response
                .Where(x => context.SearchString == null ||
                            x.Value.Contains(context.SearchString, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
