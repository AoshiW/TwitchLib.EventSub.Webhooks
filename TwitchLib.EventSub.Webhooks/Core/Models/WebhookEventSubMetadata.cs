using Microsoft.AspNetCore.Http;

namespace TwitchLib.EventSub.Webhooks.Core.Models;

public class WebhookEventSubMetadata
{
    public string MessageId { get; set; }
    public string MessageRetry { get; set; }
    public string MessageType { get; set; }
    public string MessageSignature { get; set; }
    public string MessageTimestamp { get; set; }
    public string SubscriptionType { get; set; }
    public string SubscriptionVersion { get; set; }

    // TODO Q: should we add `Dictionary<string,string>` for other unused headers?

    internal static WebhookEventSubMetadata CreateMetadata(IHeaderDictionary headers)
    {
        return new WebhookEventSubMetadata()
        {
            MessageId = headers["Twitch-Eventsub-Message-Id"]!,
            MessageRetry = headers["Twitch-Eventsub-Message-Retry"]!,
            MessageType = headers["Twitch-Eventsub-Message-Type"]!,
            MessageSignature = headers["Twitch-Eventsub-Message-Signature"]!,
            MessageTimestamp = headers["Twitch-Eventsub-Message-Timestamp"]!,
            SubscriptionType = headers["Twitch-Eventsub-Subscription-Type"]!,
            SubscriptionVersion = headers["Twitch-Eventsub-Subscription-Version"]!,
        };
    }
}
