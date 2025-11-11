using Apps.Utilities.Polling.Models.Memory;
using Apps.Utilities.Polling.Models.Request;
using Apps.Utilities.Polling.Models.Response;
using Blackbird.Applications.Sdk.Common.Polling;
using CodeHollow.FeedReader;

namespace Apps.Utilities.Polling;

[PollingEventList]
public class PollingList
{
    [PollingEvent("On time interval passed", "This event triggers consistently when the configured time interval elapses. Can be used as an alternative to a scheduled trigger.")]
    public async Task<PollingEventResponse<DateMemory, DateTime>> OnTimeIntervalPassed(PollingEventRequest<DateMemory> request,
        [PollingEventParameter] TimeIntervalPassedRequest timeIntervalPassedRequest)
    {
        var now = DateTime.UtcNow;
        var currentTimeOfDay = new TimeSpan(now.Hour, now.Minute, 0);

        if (!ShouldTriggerInTimeWindow(timeIntervalPassedRequest, currentTimeOfDay))
        {
            return new()
            {
                FlyBird = false,
                Memory = new()
                {
                    LastInteractionDate = now
                }
            };
        }

        return new()
        {
            FlyBird = true,
            Result = DateTime.UtcNow,
            Memory = new()
            {
                LastInteractionDate = DateTime.UtcNow
            }
        };
    }

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

    private bool ShouldTriggerInTimeWindow(TimeIntervalPassedRequest request, TimeSpan currentTime)
    {
        if (string.IsNullOrEmpty(request.ActiveFromTime) && string.IsNullOrEmpty(request.ActiveUntilTime))
        {
            return true;
        }

        TimeSpan? startTime = null;
        if (!string.IsNullOrEmpty(request.ActiveFromTime) &&
            TimeSpan.TryParse(request.ActiveFromTime, out var parsedStartTime))
        {
            startTime = parsedStartTime;
        }

        TimeSpan? endTime = null;
        if (!string.IsNullOrEmpty(request.ActiveUntilTime) &&
            TimeSpan.TryParse(request.ActiveUntilTime, out var parsedEndTime))
        {
            endTime = parsedEndTime;
        }

        // 1. Only start time specified - trigger if current time is after start time
        if (startTime.HasValue && !endTime.HasValue)
        {
            return currentTime >= startTime.Value;
        }

        // 2. Only end time specified - trigger if current time is before end time
        if (!startTime.HasValue && endTime.HasValue)
        {
            return currentTime <= endTime.Value;
        }

        // 3. Both times specified - handle normal case and crossing midnight
        if (startTime.HasValue && endTime.HasValue)
        {
            if (startTime.Value < endTime.Value)
            {
                // Normal case: 09:00 - 17:00
                return currentTime >= startTime.Value && currentTime <= endTime.Value;
            }
            else
            {
                // Crossing midnight: 22:00 - 06:00
                return currentTime >= startTime.Value || currentTime <= endTime.Value;
            }
        }
        return false;
    }
}