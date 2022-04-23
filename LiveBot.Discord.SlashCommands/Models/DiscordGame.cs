using Discord;
using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Discord.SlashCommands.Models
{
    /// <summary>
    /// Represents a Discord Game
    /// </summary>
    public class DiscordGame : BaseLiveBotGame
    {
        public DiscordGame(ServiceEnum serviceType, StreamingGame game) : base("https://discord.com", serviceType)
        {
            Id = "0";
            Name = game.Name;
            ThumbnailURL = "";
        }
    }
}