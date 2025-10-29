using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Files;
using System.ComponentModel;

namespace Apps.Utilities.Models.XMLFiles
{
    public class RemoveTargetTextRequest
    {
        [Display("XLIFF file")]
        public FileReference File { get; set; }

        [Display("Only targets with state"), Description("Process only <target> elements whose @state equals any of these values (case-insensitive). Leave empty to process all.")]
        [StaticDataSource(typeof(XliffInteroperableStatesDataSourceHandler))]
        public IEnumerable<string>? TargetStates { get; set; }
    }
}
