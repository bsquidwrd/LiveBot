using LiveBot.Core.Repository.Interfaces;

namespace LiveBot.Discord.Services.LiveBot
{
    public class TwitchStreamChannel : IStreamChannel
    {
        public string Site { get; }
        public string Name { get; }
        public TwitchStreamChannel(string Site, string Name)
        {
            this.Site = Site;
            this.Name = Name;
        }
    }
}
