using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.Files;

public class CsvFile
{
    [Display("CSV file")]
    public FileReference File { get; set; }
}
