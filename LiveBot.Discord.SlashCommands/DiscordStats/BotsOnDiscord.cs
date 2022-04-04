using Discord.WebSocket;

namespace LiveBot.Discord.SlashCommands.DiscordStats
{
    /// <inheritdoc/>
    public class BotsOnDiscord : BaseStats
    {
        public BotsOnDiscord(ILogger<BotsOnDiscord> logger, IConfiguration configuration, DiscordShardedClient discordClient)
            : base(logger, configuration, discordClient)
        {
            SiteName = "BotsOnDiscord";
            ApiConfigName = "BotsOnDiscord_API";
            UpdateUrl = "https://bots.ondiscord.xyz/bot-api/bots/{BotId}/guilds";
            GuildCountFieldName = "guildCount";
        }

        /// <inheritdoc/>
        public override bool SetAuthorization(HttpClient httpClient, string apiKey) =>
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
    }
}