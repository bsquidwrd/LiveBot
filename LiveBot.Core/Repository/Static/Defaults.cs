using System;

namespace LiveBot.Core.Repository.Static
{
    public static class Defaults
    {
        public static readonly TimeSpan MessageTimeout = TimeSpan.FromMinutes(1);
        public static readonly string NotificationMessage = "{role} {name} is live and playing {game}! {url}";
    }
}