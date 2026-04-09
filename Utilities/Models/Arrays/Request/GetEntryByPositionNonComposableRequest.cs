using Apps.Utilities.DataSourceHandlers;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.Models.Arrays.Request;

public class GetEntryByPositionNonComposableRequest
{
    [DataSource(typeof(ArrayEmptyDataSource))]
    public IEnumerable<string> Array { get; set; }
}