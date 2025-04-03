using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Models.Files;
public class ColumnIndexesRequest
{
    [Display("Column indexes", Description = "The first column starts with 0")]
    public IEnumerable<int> ColumnIndexes { get; set; }
}
