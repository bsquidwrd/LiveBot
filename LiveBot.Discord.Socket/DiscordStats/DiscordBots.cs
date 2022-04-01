using Discord.WebSocket;

namespace LiveBot.Discord.Socket.DiscordStats
{
    /// <inheritdoc/>
    public class DiscordBots : BaseStats
    {
        public DiscordBots(ILogger<DiscordBots> logger, IConfiguration configuration, DiscordShardedClient discordClient)
            : base(logger, configuration, discordClient)
        {
            SiteName = "DiscordBots";
            ApiConfigName = "DiscordBots_API";
            UpdateUrl = "https://discord.bots.gg/api/v1/bots/{BotId}/stats";
            GuildCountFieldName = "guildCount";
        }

        /// <inheritdoc/>
        public override bool SetAuthorization(HttpClient httpClient, string apiKey) =>
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
    }
}