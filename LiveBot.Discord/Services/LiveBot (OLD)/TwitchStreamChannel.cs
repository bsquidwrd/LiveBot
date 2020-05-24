using System.Text.RegularExpressions;

namespace LiveBot.Discord.Services.LiveBot
{
    public class TwitchStreamChannel : BaseStreamChannel
    {
        public static string URLPATTERN = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?twitch\\.tv/(?<username>[a-zA-Z0-9_]{1,})";

        public TwitchStreamChannel(string StreamURL) : base(StreamURL, URLPATTERN)
        {
        }

        public override bool IsValid()
        {
            return Regex.IsMatch(_StreamURL, _URLPattern);
        }

        public override string GetUsername()
        {
            return _URLRegex.Match(_StreamURL).Groups["username"].ToString();
        }
    }
}