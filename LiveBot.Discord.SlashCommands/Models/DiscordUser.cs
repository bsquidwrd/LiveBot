using Discord.WebSocket;
using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Discord.SlashCommands.Models
{
    /// <summary>
    /// Represents a Discord User
    /// </summary>
    public class DiscordUser : BaseLiveBotUser
    {
        public DiscordUser(ServiceEnum serviceType, SocketGuildUser user) : base("https://discord.com", serviceType)
        {
            Id = user.Id.ToString();
            Username = user.Username;
            DisplayName = user.DisplayName;
            AvatarURL = user.GetDisplayAvatarUrl() ?? user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
            ProfileURL = "";
        }
    }
}