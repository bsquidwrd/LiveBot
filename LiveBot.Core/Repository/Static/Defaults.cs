using System;

namespace LiveBot.Core.Repository.Static
{
    public static class Defaults
    {
        public static TimeSpan MessageTimeout = TimeSpan.FromMinutes(1);
        public static string NotificationMessage = "{role} {name} is live and is playing {game}! {url}";
    }
}