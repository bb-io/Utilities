using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.DataSourceHandlers
{
    public class VersionTypeSourceHandler : BaseInvocable, IDataSourceHandler
    {
        private IEnumerable<AuthenticationCredentialsProvider> Creds =>
            InvocationContext.AuthenticationCredentialsProviders;

        public VersionTypeSourceHandler(InvocationContext invocationContext) : base(invocationContext)
        {
        }

        public Dictionary<string, string> GetData(DataSourceContext context)
        {
            return new Dictionary<string, string>()
        {
            {"major", "Major" },
            {"minor", "Minor" },
            {"patch", "Patch" }
        }.Where(x => string.IsNullOrWhiteSpace(context.SearchString) || x.Key.Contains(context.SearchString)).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
