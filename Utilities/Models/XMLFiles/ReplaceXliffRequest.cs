
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles
{
    public class ReplaceXliffRequest
    {
        [Display("XLIFF file")]
        public FileReference File { get; set; }

        [Display("Delete target")]
        public bool? DeleteTargets { get; set; }

        [Display("Set new target language")]
        public string? SetNewTargetLanguage { get; set; }
    }
}
