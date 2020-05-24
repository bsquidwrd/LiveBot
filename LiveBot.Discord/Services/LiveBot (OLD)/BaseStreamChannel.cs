using System.Text.RegularExpressions;

namespace LiveBot.Discord.Services.LiveBot
{
    public abstract class BaseStreamChannel
    {
        public readonly string _StreamURL;
        public readonly string _URLPattern;
        public readonly Regex _URLRegex;

        public BaseStreamChannel(string StreamURL, string URLPattern)
        {
            _StreamURL = StreamURL;
            _URLPattern = URLPattern;
            _URLRegex = new Regex(URLPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled); ;
        }

        public abstract bool IsValid();

        public abstract string GetUsername();
    }
}