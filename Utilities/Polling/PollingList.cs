using Apps.Utilities.Polling.Models.Memory;
using Apps.Utilities.Polling.Models.Request;
using Apps.Utilities.Polling.Models.Response;
using Blackbird.Applications.Sdk.Common.Polling;
using CodeHollow.FeedReader;

namespace Apps.Utilities.Polling;

[PollingEventList]
public class PollingList
{
    [PollingEvent("On RSS feed changed", "On a new version of the RSS feed published")]
    public async Task<PollingEventResponse<DateMemory, RssFeedResponse>> OnFeedChanged(
        PollingEventRequest<DateMemory> request, [PollingEventParameter] RssFeedRequest feedInput)
    {
        if (request.Memory is null)
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastInteractionDate = DateTime.UtcNow
                }
            };
        }

        var feed = await FeedReader.ReadAsync(feedInput.FeedUrl);

        if (feed.LastUpdatedDate < request.Memory.LastInteractionDate)
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastInteractionDate = DateTime.UtcNow
                }
            };
        }

        return new()
        {
            FlyBird = true,
            Result = new(feed, request.Memory.LastInteractionDate),
            Memory = new()
            {
                LastInteractionDate = DateTime.UtcNow
            }
        };
    }
}