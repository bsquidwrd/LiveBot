using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Core.Repository.Models.Streams
{
    public class StreamSubscription : BaseModel<StreamSubscription>
    {
        public StreamUser User { get; set; }
        public DiscordChannel DiscordChannel { get; set; }
        public DiscordRole DiscordRole { get; set; }
        public string Message { get; set; }
    }
}