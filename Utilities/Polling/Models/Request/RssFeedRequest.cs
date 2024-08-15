using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Polling.Models.Request;

public class RssFeedRequest
{
    [Display("Feed URL")]
    public string FeedUrl { get; set; }
}