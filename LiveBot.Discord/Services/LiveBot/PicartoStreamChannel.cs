using System.Text.RegularExpressions;

namespace LiveBot.Discord.Services.LiveBot
{
    public class PicartoStreamChannel : BaseStreamChannel
    {
        public static string URLPATTERN = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?picarto\\.tv/(?<username>[a-zA-Z0-9_]{1,})";

        public PicartoStreamChannel(string StreamURL) : base(StreamURL, URLPATTERN)
        {
        }

        public override bool IsValid()
        {
            return false;
            return Regex.IsMatch(_StreamURL, _URLPattern);
        }

        public override string GetUsername()
        {
            return _URLRegex.Match(_StreamURL).Groups["username"].ToString();
        }
    }
}