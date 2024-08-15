using Blackbird.Applications.Sdk.Common;
using CodeHollow.FeedReader;

namespace Apps.Utilities.Polling.Models.Response;

public class FeedItemResponse
{
    public string Title { get; set; }

    public string Description { get; set; }

    public string Link { get; set; }

    [Display("Publishing date")] public DateTime? PublishingDate { get; set; }

    public string? Content { get; set; }

    public FeedItemResponse(FeedItem feedItem)
    {
        Title = feedItem.Title;
        Description = feedItem.Description;
        Link = feedItem.Link;
        PublishingDate = feedItem.PublishingDate;
        Content = feedItem.Content;
    }
}