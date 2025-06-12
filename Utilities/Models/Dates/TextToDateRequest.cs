using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dictionaries;

namespace Apps.Utilities.Models.Dates
{
    public class TextToDateRequest
    {
        public string Text { get; set; }

        [Display("Culture")]
        [StaticDataSource(typeof(CultureSourceHandler))]
        public string? Culture { get; set; }

        [Display("Timezone")]
        [StaticDataSource(typeof(TimeZoneSourceHandler))]
        public string? Timezone { get; set; }

        [Display("Date format")]
        [StaticDataSource(typeof(DateFormatSourceHandler))]
        public string? Format { get; set; }
    }
}
