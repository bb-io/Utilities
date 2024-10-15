﻿using Apps.Utilities.DataSourceHandlers;
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
    }
}