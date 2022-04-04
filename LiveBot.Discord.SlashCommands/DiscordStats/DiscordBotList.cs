using Discord.WebSocket;

namespace LiveBot.Discord.SlashCommands.DiscordStats
{
    /// <inheritdoc/>
    public class DiscordBotList : BaseStats
    {
        public DiscordBotList(ILogger<DiscordBotList> logger, IConfiguration configuration, DiscordShardedClient discordClient)
            : base(logger, configuration, discordClient)
        {
            SiteName = "DiscordBotList";
            ApiConfigName = "DiscordBotList_API";
            UpdateUrl = "https://discordbotlist.com/api/v1/bots/{BotId}/stats";
            GuildCountFieldName = "guilds";
        }

        /// <inheritdoc/>
        public override bool SetAuthorization(HttpClient httpClient, string apiKey) =>
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
    }
}