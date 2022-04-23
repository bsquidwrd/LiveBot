using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Base.Monitor;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Discord.SlashCommands.Models
{
    /// <summary>
    /// Represents a Discord Stream
    /// </summary>
    public class DiscordStream : BaseLiveBotStream
    {
        public DiscordStream(ServiceEnum serviceType, IDiscordMemberLive stream, ILiveBotUser user, ILiveBotGame game) : base("https://discord.com", serviceType)
        {
            UserId = user.Id;
            User = user;
            Id = stream.DiscordUserId.ToString();
            Title = stream.GameDetails;
            StartTime = stream.LiveTime;
            GameId = game.Id;
            Game = game;
            ThumbnailURL = "";
            StreamURL = stream.Url;
        }
    }
}