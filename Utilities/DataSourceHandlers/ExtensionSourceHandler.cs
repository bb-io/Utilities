using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.DataSourceHandlers
{
    public class ExtensionSourceHandler : BaseInvocable, IDataSourceHandler
    {
        public ExtensionSourceHandler(InvocationContext invocationContext) : base(invocationContext)
        {
        }

        public Dictionary<string, string> GetData(DataSourceContext context)
        {
            var extension = new List<string>
        {
            ".txt",
            ".doc",
            ".docx"
        };

            return extension
                .Where(ext => context.SearchString == null || ext.Contains(context.SearchString,
                    StringComparison.OrdinalIgnoreCase))
                .ToDictionary(ext => ext, ext => ext);
        }
    }
}
