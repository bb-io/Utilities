using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Models.Dates
{
    public class GenerateDateRequest
    {
        [Display("Add days")]
        public double? AddDays { get; set; }

        [Display("Add hours")]
        public double? AddHours { get; set; }

        [Display("Add minutes")]
        public double? AddMinutes { get; set; }
    }
}
