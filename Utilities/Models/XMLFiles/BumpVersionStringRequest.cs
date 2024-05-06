using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;
using Blackbird.Applications.Sdk.Common.Dynamic;


namespace Apps.Utilities.Models.XMLFiles
{
    public class BumpVersionStringRequest
    {
        [Display("Version string")]
        public string VersionString { get; set; }

        [Display("Version type")]
        [StaticDataSource(typeof(VersionTypeSourceHandler))]
        public string VersionType { get; set; }
    }
}
