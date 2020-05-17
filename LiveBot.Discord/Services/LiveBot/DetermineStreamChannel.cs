namespace LiveBot.Discord.Services.LiveBot
{
    public class DetermineStreamChannel
    {
        private readonly string StreamURL;

        public DetermineStreamChannel(string StreamURL)
        {
            this.StreamURL = StreamURL;
        }

        public BaseStreamChannel Check()
        {
            TwitchStreamChannel twitchStreamChannel = new TwitchStreamChannel(StreamURL);
            MixerStreamChannel mixerStreamChannel = new MixerStreamChannel(StreamURL);
            FacebookStreamChannel facebookStreamChannel = new FacebookStreamChannel(StreamURL);
            YouTubeStreamChannel youtubeStreamChannel = new YouTubeStreamChannel(StreamURL);
            PicartoStreamChannel picartoStreamChannel = new PicartoStreamChannel(StreamURL);

            // Check Twitch
            if (twitchStreamChannel.IsValid())
            {
                return twitchStreamChannel;
            }
            // Check Mixer
            else if (mixerStreamChannel.IsValid())
            {
                return mixerStreamChannel;
            }
            // Check Facebook
            else if (facebookStreamChannel.IsValid())
            {
                return facebookStreamChannel;
            }
            // Check YouTube
            else if (youtubeStreamChannel.IsValid())
            {
                return youtubeStreamChannel;
            }
            // Check Picarto
            else if (picartoStreamChannel.IsValid())
            {
                return picartoStreamChannel;
            }
            return null;
        }
    }
}