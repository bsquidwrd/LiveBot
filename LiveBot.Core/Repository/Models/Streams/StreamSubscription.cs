using LiveBot.Core.Repository.Models.Discord;

namespace LiveBot.Core.Repository.Models.Streams
{
    public class StreamSubscription : BaseModel<StreamSubscription>
    {
        public virtual StreamUser User { get; set; }
        public virtual DiscordGuild DiscordGuild { get; set; }
        public virtual DiscordChannel DiscordChannel { get; set; }
        public virtual DiscordRole DiscordRole { get; set; }
        public string Message { get; set; }
        public bool IsFromRole { get; set; }
        public ulong? DiscordUserId { get; set; }
    }
}