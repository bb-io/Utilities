using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers;

public class DateFormatSourceHandler : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>
        {
            new DataSourceItem("M/d/yyyy H:mm:ss", "M/d/yyyy H:mm:ss"),
            new DataSourceItem("MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss"),
            new DataSourceItem("dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss"),
            new DataSourceItem("yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss"),
            new DataSourceItem("yyyy/MM/dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss"),

            new DataSourceItem("M/d/yyyy", "M/d/yyyy"),
            new DataSourceItem("MM/dd/yyyy", "MM/dd/yyyy"),
            new DataSourceItem("dd/MM/yyyy", "dd/MM/yyyy"),
            new DataSourceItem("yyyy-MM-dd", "yyyy-MM-dd"),
            new DataSourceItem("yyyy/MM/dd", "yyyy/MM/dd"),

            new DataSourceItem("M/d/yyyy H:mm:ss zzz", "M/d/yyyy H:mm:ss zzz"),
            new DataSourceItem("MM/dd/yyyy HH:mm:ss zzz", "MM/dd/yyyy HH:mm:ss zzz"),
            new DataSourceItem("dd/MM/yyyy HH:mm:ss zzz", "dd/MM/yyyy HH:mm:ss zzz"),
            new DataSourceItem("yyyy-MM-dd HH:mm:ss zzz", "yyyy-MM-dd HH:mm:ss zzz"),
            new DataSourceItem("yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-ddTHH:mm:sszzz"),
            new DataSourceItem("yyyy-MM-ddTHH:mm:ss.fffzzz", "yyyy-MM-ddTHH:mm:ss.fffzzz"),
            new DataSourceItem("yyyy-MM-ddTHH:mm:ss.fff", "ISO format with milliseconds"),
            new DataSourceItem("M/d/yyyy H:mm:ss.fff", "M/d/yyyy H:mm:ss.fff"),
            new DataSourceItem("MM/dd/yyyy HH:mm:ss.fff", "MM/dd/yyyy HH:mm:ss.fff"),
            new DataSourceItem("dd/MM/yyyy HH:mm:ss.fff", "dd/MM/yyyy HH:mm:ss.fff"),
            new DataSourceItem("yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-dd HH:mm:ss.fff"),
            new DataSourceItem("yyyy/MM/dd HH:mm:ss.fff", "yyyy/MM/dd HH:mm:ss.fff"),

            new DataSourceItem  ("OADate", "OLE Automation date"),
            new DataSourceItem  ("d", "Short date"),
            new DataSourceItem  ("D", "Long date"),
            new DataSourceItem  ("f", "Full date/time (short time)"),
            new DataSourceItem  ("F", "Full date/time (long time)"),
            new DataSourceItem  ("g", "General date/time (short time)"),
            new DataSourceItem  ("G", "General date/time (long time)"),
            new DataSourceItem  ("M", "Month/day"),
            new DataSourceItem  ("R", "RFC1123"),
            new DataSourceItem  ("t", "Short time"),
            new DataSourceItem  ("T", "Long time"),
            new DataSourceItem  ("U", "Universal full date/time"),
            new DataSourceItem  ("Y", "Year month")
            
        };
    }
}