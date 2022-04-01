using Discord.WebSocket;

namespace LiveBot.Discord.Socket.DiscordStats
{
    /// <inheritdoc/>
    public class TopGG : BaseStats
    {
        public TopGG(ILogger<TopGG> logger, IConfiguration configuration, DiscordShardedClient discordClient)
            : base(logger, configuration, discordClient)
        {
            SiteName = "TopGG";
            ApiConfigName = "TopGG_API";
            UpdateUrl = "https://top.gg/api/bots/{BotId}/stats";
            GuildCountFieldName = "server_count";
        }

        /// <inheritdoc/>
        public override bool SetAuthorization(HttpClient httpClient, string apiKey) =>
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
    }
}