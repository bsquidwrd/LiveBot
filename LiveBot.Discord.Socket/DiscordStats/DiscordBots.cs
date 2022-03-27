using Discord;
using Discord.WebSocket;
using LiveBot.Core.Interfaces.Discord;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveBot.Discord.Socket.DiscordStats
{
    internal class DiscordBotsPayload
    {
        [JsonPropertyName("guildCount")]
        internal int guildCount;

        public DiscordBotsPayload(int count)
        {
            guildCount = count;
        }
    }

    public class DiscordBots : IDiscordStats
    {
        private readonly ILogger<DiscordBots> _logger;
        private readonly IConfiguration _configuration;
        private readonly DiscordShardedClient _discordClient;
        private System.Timers.Timer? _timer = null;
        private readonly bool IsDebug = false;

        private readonly string SiteName = "DiscordBots";
        private readonly string ApiConfigName = "DiscordBots_API";
        private readonly string UpdateUrl = "https://discord.bots.gg/api/v1/bots/{BotId}/stats";

        public DiscordBots(ILogger<DiscordBots> logger, IConfiguration configuration, DiscordShardedClient discordClient)
        {
            _logger = logger;
            _configuration = configuration;
            _discordClient = discordClient;

            IsDebug = _configuration.GetValue<bool>("IsDebug", false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!IsDebug)
            {
                TimeSpan timeSpan = TimeSpan.FromMinutes(5);
                var timer = new System.Timers.Timer(timeSpan.TotalMilliseconds)
                {
                    AutoReset = true
                };
                timer.Elapsed += async (sender, e) => await UpdateStats();
                _timer = timer;
                _timer.Start();
            }
            await Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            _timer = null;
            return Task.CompletedTask;
        }

        public async Task UpdateStats()
        {
            if (IsDebug || _discordClient.LoginState != LoginState.LoggedIn || _discordClient.CurrentUser?.Id == null)
                return;

            var guilds = _discordClient.Guilds;
            var payload = new DiscordBotsPayload(guilds.Count);
            var apiKey = _configuration.GetValue<string>(ApiConfigName);
            if (apiKey == null)
                return;

            HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var endpoint = UpdateUrl.Replace("{BotId}", _discordClient.CurrentUser.Id.ToString());
                var response = await httpClient.PostAsync(requestUri: endpoint, content: content);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation(message: "Updated Guild Count for {StatsSiteName}: {GuildCount}", SiteName, guilds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Unable to update stats for {StatsSiteName}", SiteName);
            }
            finally
            {
                httpClient.Dispose();
            }
        }
    }
}