using LiveBot.Core.Contracts.Discord;

namespace LiveBot.Discord.Contracts
{
    public class DiscordAlert : IDiscordAlert
    {
        public string Message { get; set; }
    }
}