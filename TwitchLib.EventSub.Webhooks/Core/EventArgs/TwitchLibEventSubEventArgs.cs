using System.Collections.Generic;
using TwitchLib.EventSub.Webhooks.Core.Models;

namespace TwitchLib.EventSub.Webhooks.Core.EventArgs
{
    public abstract class TwitchLibEventSubEventArgs<T> : System.EventArgs where T: new()
    {
        public WebhookEventSubMetadata Headers { get; set; } = new(); // TODO rename to Metadata
        public T Notification { get; set; } = new();
    }
}