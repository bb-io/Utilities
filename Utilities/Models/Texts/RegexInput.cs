﻿using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts
{
    public class RegexInput
    {
        public string Regex { get; set; }

        [Display("Group Number")]
        public string Group { get; set; }
    }
}
