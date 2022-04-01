using Discord.WebSocket;

namespace LiveBot.Discord.Socket.DiscordStats
{
    /// <inheritdoc/>
    public class BotsForDiscord : BaseStats
    {
        public BotsForDiscord(ILogger<BotsForDiscord> logger, IConfiguration configuration, DiscordShardedClient discordClient)
            : base(logger, configuration, discordClient)
        {
            SiteName = "BotsForDiscord";
            ApiConfigName = "BotsForDiscord_API";
            UpdateUrl = "https://discords.com/bots/api/bot/{BotId}";
            GuildCountFieldName = "server_count";
        }

        /// <inheritdoc/>
        public override bool SetAuthorization(HttpClient httpClient, string apiKey) =>
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
    }
}