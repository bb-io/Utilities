using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apps.Utilities.Connections
{
    public class ConnectionValidator : IConnectionValidator
    {
        public ValueTask<ConnectionValidationResponse> ValidateConnection(
       IEnumerable<AuthenticationCredentialsProvider> authProviders, CancellationToken cancellationToken)
        {
            return new(Task.FromResult(new ConnectionValidationResponse()
            {
                IsValid = true
            }));
        }
    }
}
