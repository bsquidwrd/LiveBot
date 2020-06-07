using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Core.Repository.Models.Streams
{
    public class StreamSubscription : BaseModel<StreamSubscription>
    {
        public ServiceEnum ServiceType { get; set; }
        public DiscordGuild Guild { get; set; }
        public DiscordChannel Channel { get; set; }
        public DiscordRole Role { get; set; }
        public string SourceID { get; set; }
        public string Message { get; set; }
    }
}