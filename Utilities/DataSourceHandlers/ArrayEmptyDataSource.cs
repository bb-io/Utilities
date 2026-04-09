using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.Utilities.DataSourceHandlers;

public class ArrayEmptyDataSource : IAsyncDataSourceItemHandler
{
    public Task<IEnumerable<DataSourceItem>> GetDataAsync(DataSourceContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<DataSourceItem>
        {
            new(string.Empty, "Nothing to fetch, please ignore and use the array input as normal.")
        }.AsEnumerable());
    }
}