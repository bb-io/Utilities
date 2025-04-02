using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Models.Files;
public class CompareFilesRequest
{
    [Display("Files to compare")]
    public IEnumerable<FileReference> Files { get; set; }
}
