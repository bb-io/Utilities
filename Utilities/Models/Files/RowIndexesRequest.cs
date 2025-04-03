using Blackbird.Applications.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Models.Files;
public class RowIndexesRequest
{
    [Display("Row indexes", Description = "The first row starts with 0")]
    public IEnumerable<int> RowIndexes { get; set; }
}
