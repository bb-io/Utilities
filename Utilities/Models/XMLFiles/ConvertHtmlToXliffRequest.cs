﻿using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles
{
    public class ConvertHtmlToXliffRequest
    {
        [Display("HTML file")]
        public FileReference File { get; set; }

        [Display("Source language")]
        public string SourceLanguage { get; set; }

        [Display("Target language")]
        public string TargetLanguage { get; set; }
    }
}
