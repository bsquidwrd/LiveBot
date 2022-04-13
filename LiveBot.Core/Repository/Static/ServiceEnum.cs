using System;
using System.Text.RegularExpressions;

namespace LiveBot.Core.Repository.Static
{
    /// <summary>
    /// Represents a Monitoring Service
    /// </summary>
    public enum ServiceEnum
    {
        None = 0,
        Twitch = 1,
        YouTube = 2,
        Trovo = 3,
    }

    public static class ServiceUtils
    {
        public static uint GetAlertColor(this ServiceEnum serviceEnum) =>
            serviceEnum switch
            {
                ServiceEnum.Twitch => 0x9146FF,
                ServiceEnum.YouTube => 0xF80000,
                ServiceEnum.Trovo => 0x37BB76,
                _ => 0xFFFFFF,
            };

        public const string TwitchUrlPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?twitch\\.tv/(?<username>[a-zA-Z0-9_]{1,})";
        public const string YouTubeUrlPattern = "^((?:https?:)?\\/\\/)?((?:www|m)\\.)?((?:youtube(-nocookie)?\\.com|youtu.be))(\\/(?:[\\w\\-]+\\?v=|embed\\/|v\\/)?)([\\w\\-]+)(\\S+)?$";
        public const string TrovoUrlPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?trovo\\.live/(?<username>[a-zA-Z0-9_]{1,})";

        public static ServiceEnum ToServiceEnum(this Uri uri)
        {
            if (Regex.IsMatch(uri.AbsoluteUri, TwitchUrlPattern))
                return ServiceEnum.Twitch;
            else if (Regex.IsMatch(uri.AbsoluteUri, YouTubeUrlPattern))
                return ServiceEnum.YouTube;
            else if (Regex.IsMatch(uri.AbsoluteUri, TrovoUrlPattern))
                return ServiceEnum.Trovo;
            else
                return ServiceEnum.None;
        }
    }
}