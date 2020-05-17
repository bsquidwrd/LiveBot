using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Discord.Services.LiveBot;
using Serilog;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LiveBot.Discord.TypeReaders
{
    public class StreamChannelTypeReader : TypeReader
    {
        /// <inheritdoc />
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            string Site;
            string Username;
            IStreamChannel StreamChannel;
            input = input.Trim();

            const string TwitchURLPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?twitch\\.tv/(?<username>[a-zA-Z0-9_]{1,})";
            //const string MixerURLPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?mixer\\.com/(?<username>[a-zA-Z0-9_]{1,})";
            //const string FacebookURLPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?facebook\\.com/(?<username>[a-zA-Z0-9_]{1,})";
            //const string YouTubeURLPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?youtube\\.com/channel/(?<username>[a-zA-Z0-9_]{1,})";
            //const string PicartoURLPattern = "^((http|https):\\/\\/|)([\\w\\d]+\\.)?picarto\\.tv/(?<username>[a-zA-Z0-9_]{1,})";

            Regex TwitchURLRegex = new Regex(TwitchURLPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            //Regex MixerURLRegex = new Regex(MixerURLPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            //Regex FacebookURLRegex = new Regex(FacebookURLPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            //Regex YouTubeURLRegex = new Regex(YouTubeURLPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            //Regex PicartoURLRegex = new Regex(PicartoURLPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // Check Twitch
            if (Regex.IsMatch(input, TwitchURLPattern))
            {
                Match urlMatch = TwitchURLRegex.Match(input);
                Site = "Twitch";
                Username = urlMatch.Groups["username"].ToString();
                StreamChannel = new TwitchStreamChannel(Site, Username);
            }
            //// Check Mixer
            //else if (Regex.IsMatch(input, MixerURLPattern))
            //{
            //    Match urlMatch = MixerURLRegex.Match(input);
            //    site = "Mixer";
            //    username = urlMatch.Groups["username"].ToString();
            //    StreamChannel = new MixerStreamChannel(Site, Username);
            //}
            //// Check Facebook
            //else if (Regex.IsMatch(input, FacebookURLPattern))
            //{
            //    Match urlMatch = FacebookURLRegex.Match(input);
            //    site = "Facebook";
            //    username = urlMatch.Groups["username"].ToString();
            //    StreamChannel = new FacebookStreamChannel(Site, Username);
            //}
            //// Check YouTube
            //else if (Regex.IsMatch(input, YouTubeURLPattern))
            //{
            //    Match urlMatch = YouTubeURLRegex.Match(input);
            //    site = "YouTube";
            //    username = urlMatch.Groups["username"].ToString();
            //    StreamChannel = new YouTubeStreamChannel(Site, Username);
            //}
            //// Check Picarto
            //else if (Regex.IsMatch(input, PicartoURLPattern))
            //{
            //    Match urlMatch = PicartoURLRegex.Match(input);
            //    site = "Picarto";
            //    username = urlMatch.Groups["username"].ToString();
            //    StreamChannel = new PicartoStreamChannel(Site, Username);
            //}
            else
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed,
                    $@"{context.Message.Author.Mention}, you must provide a valid link to the stream you want to monitor."));
            }

            return Task.FromResult(TypeReaderResult.FromSuccess(StreamChannel));
        }
    }
}