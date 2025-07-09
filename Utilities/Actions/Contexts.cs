using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.Sdk.Common;
using Apps.Utilities.Models.Contexts;

namespace Apps.Utilities.Actions
{
    [ActionList("Miscellaneous")]
    public class Contexts : BaseInvocable
    {
        public Contexts(InvocationContext invocationContext) : base(invocationContext)
        {
        }

        [Action("Get flight context", Description = "Get various variables that have to do with the flight including IDs, URLs, etc.")]
        public FlightContext GetFlightContext()
        {
            return new()
            {
                FlightId = InvocationContext.Flight?.Id,
                FlightUrl = InvocationContext.Flight?.Url,
                BirdId = InvocationContext.Bird?.Id.ToString(),
                BirdName = InvocationContext.Bird?.Name,
                NestId = InvocationContext.Workspace?.Id.ToString(),
                NestName = InvocationContext.Workspace?.Name
            };
        }
    }
}
