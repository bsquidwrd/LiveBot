namespace LiveBot.Core.Repository.Models.Streams
{
    public class RoleToMention : BaseModel<RoleToMention>
    {
        public virtual StreamSubscription StreamSubscription { get; set; }
        public virtual ulong DiscordRoleId { get; set; }
    }
}