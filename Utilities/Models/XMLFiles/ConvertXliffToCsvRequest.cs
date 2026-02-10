using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Files;

namespace Apps.Utilities.Models.XMLFiles;

public class ConvertXliffToCsvRequest
{
    [Display("File")]
    public FileReference File { get; set; }

    [Display("Batch size", Description = "Maximum characters per CSV file. Default is 10000")]
    public int? BatchSize { get; set; }
}
