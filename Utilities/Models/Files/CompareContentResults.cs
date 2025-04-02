using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Models.Files;
public class CompareContentResults
{
    [Display("File contents are equal")]
    public bool AreEqual { get; set; } = true;
}
