using Blackbird.Applications.Sdk.Common;
using CodeHollow.FeedReader;

namespace Apps.Utilities.Polling.Models.Response;

public class RssFeedResponse
{
    public string Title { get; set; }

    public string Description { get; set; }

    public string Link { get; set; }

    [Display("Last updated")] public DateTime? LastUpdated { get; set; }

    [Display("New entries")] public IEnumerable<FeedItemResponse> NewEntries { get; set; }

    public RssFeedResponse(Feed feed, DateTime lastInteractionDate)
    {
        Title = feed.Title;
        Description = feed.Description;
        Link = feed.Link;
        LastUpdated = feed.LastUpdatedDate;
        NewEntries = feed.Items
            .Where(x => x.PublishingDate > lastInteractionDate)
            .Select(x => new FeedItemResponse(x))
            .ToArray();
    }
}