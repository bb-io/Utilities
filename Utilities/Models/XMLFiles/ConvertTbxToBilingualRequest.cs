using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles
{
    public class ConvertTbxToBilingualRequest
    {
        [Display("Glossary", Description = "Glossary file in TBX format (exported by Blackbird from another app)")]
        public FileReference File { get; set; }

        [Display("Language 1", Description ="First language of the pair to keep. Language code is expected in lower case like \"en\" or \"fr-ca\".")]
        public string SourceLanguage { get; set; }

        [Display("Language 2", Description = "Second language of the pair to keep. Language code is expected in lower case like \"en\" or \"fr-ca\".")]
        public string TargetLanguage { get; set; }
    }
}
