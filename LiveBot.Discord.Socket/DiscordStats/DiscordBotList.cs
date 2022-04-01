using Discord.WebSocket;

namespace LiveBot.Discord.Socket.DiscordStats
{
    /// <inheritdoc/>
    public class DiscordBotList : BaseStats
    {
        public DiscordBotList(ILogger<DiscordBotList> logger, IConfiguration configuration, DiscordShardedClient discordClient)
            : base(logger, configuration, discordClient)
        {
            SiteName = "BotsForDiscord";
            ApiConfigName = "BotsForDiscord_API";
            UpdateUrl = "https://discords.com/bots/api/bot/{BotId}";
            GuildCountFieldName = "guilds";
        }

        /// <inheritdoc/>
        public override bool SetAuthorization(HttpClient httpClient, string apiKey) =>
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
    }
}