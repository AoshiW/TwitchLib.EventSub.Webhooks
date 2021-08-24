﻿using TwitchLib.EventSub.Webhooks.Core.Models;
using TwitchLib.EventSub.Webhooks.Core.SubscriptionTypes.Channel;

namespace TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel
{
    public class ChannelFollowArgs : TwitchLibEventSubEventArgs<EventSubNotificationPayload<ChannelFollow>>
    { }
}