using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.DataSourceHandlers;
public class CsvColumnCondition : IStaticDataSourceItemHandler
{
    public IEnumerable<DataSourceItem> GetData()
    {
        return new List<DataSourceItem>()
        {
            new DataSourceItem("is_empty", "Is empty"),
            new DataSourceItem("is_full", "Is full")
        };
    }
}
