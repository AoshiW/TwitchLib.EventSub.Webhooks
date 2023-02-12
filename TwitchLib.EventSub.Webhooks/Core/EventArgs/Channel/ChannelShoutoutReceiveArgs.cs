﻿using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Webhooks.Core.Models;

namespace TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel
{
    public class ChannelShoutoutReceiveArgs : TwitchLibEventSubEventArgs<EventSubNotificationPayload<ChannelShoutoutReceive>>
    { }
}