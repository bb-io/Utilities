using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Models.Files
{
    public class MultipleFilesResponse
    {
        [Display("Files")]
        public IEnumerable<FileDto> Files { get; set; }
    }
}
