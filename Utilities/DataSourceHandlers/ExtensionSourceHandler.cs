using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.DataSourceHandlers
{
    public class ExtensionSourceHandler : IStaticDataSourceHandler
    {
        public Dictionary<string, string> GetData()
        {
            var extension = new List<string>
            {
                ".txt",
                ".doc",
                ".docx"
            };

            return extension.ToDictionary(ext => ext, ext => ext);
        }
    }
}
