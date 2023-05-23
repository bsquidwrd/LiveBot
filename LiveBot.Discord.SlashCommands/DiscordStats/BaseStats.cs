using Discord;
using Discord.WebSocket;
using LiveBot.Core.Interfaces.Discord;
using System.Text;
using System.Text.Json;

namespace LiveBot.Discord.SlashCommands.DiscordStats
{
    /// <summary>
    /// Class used to update stats on a Discord Bot site
    /// </summary>
    public abstract class BaseStats : IDiscordStats
    {
        internal readonly ILogger<BaseStats> _logger;
        internal readonly IConfiguration _configuration;
        internal readonly DiscordShardedClient _discordClient;
        internal System.Timers.Timer? _timer = null;
        internal readonly bool IsDebug = false;

        /// <summary>
        /// Used for logging which site requests are for
        /// </summary>
        internal string SiteName = "";

        /// <summary>
        /// What variable to look for in <see cref="IConfiguration"/>
        /// </summary>
        internal string ApiConfigName = "";

        /// <summary>
        /// The URL to <see cref="HttpMethod.Post"/>.
        /// Must contain <c>{BotId}</c> to be replaced on send
        /// </summary>
        internal string UpdateUrl = "";

        /// <summary>
        /// The field name to set for Guild Count when sending the payload
        /// </summary>
        internal string GuildCountFieldName = "";

        /// <summary>
        /// Instantiates a new
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="discordClient"></param>
        public BaseStats(ILogger<BaseStats> logger, IConfiguration configuration, DiscordShardedClient discordClient)
        {
            _logger = logger;
            _configuration = configuration;
            _discordClient = discordClient;

            IsDebug = Convert.ToBoolean(_configuration.GetValue<string>("IsDebug") ?? "false");
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (
                !IsDebug &&
                !string.IsNullOrEmpty(SiteName) &&
                !string.IsNullOrEmpty(ApiConfigName) &&
                !string.IsNullOrEmpty(UpdateUrl)
            )
            {
                TimeSpan timeSpan = TimeSpan.FromMinutes(5);
                var timer = new System.Timers.Timer(timeSpan.Duration().TotalMilliseconds)
                {
                    AutoReset = true
                };
                timer.Elapsed += async (sender, e) => await UpdateStats();
                _timer = timer;
                _timer.Start();
            }
            else
            {
                _logger.LogInformation("Skipping starting stats service for {StatsSiteName} due to being in debug", string.IsNullOrEmpty(SiteName) ? "Unknown" : SiteName);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            _timer = null;
            _logger.LogInformation("Stopping stats service for {StatsSiteName}", SiteName);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Attempts to update the stats for this <see cref="IDiscordStats"/>
        /// </summary>
        /// <returns></returns>
        public async Task UpdateStats()
        {
            if (IsDebug || _discordClient.LoginState != LoginState.LoggedIn || _discordClient.CurrentUser?.Id == null)
                return;

            var apiKey = _configuration.GetValue<string>(ApiConfigName);
            if (apiKey == null)
                return;

            HttpClient httpClient = new();
            SetAuthorization(httpClient, apiKey);

            var guilds = _discordClient.Guilds;
            var payload = new Dictionary<string, int>
            {
                { GuildCountFieldName, guilds.Count }
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = UpdateUrl.Replace("{BotId}", _discordClient.CurrentUser.Id.ToString());
            try
            {
                var response = await httpClient.PostAsync(requestUri: endpoint, content: content);
                try
                {
                    response.EnsureSuccessStatusCode();
                    _logger.LogInformation(message: "Updated Guild Count for {StatsSiteName}: {GuildCount}", SiteName, guilds.Count);
                }
                catch (Exception ex)
                {
                    var ResponseBody = string.Empty;
                    if (response != null)
                        ResponseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError(exception: ex, message: "Unable to update stats for {StatsSiteName} {ResponseBody}", SiteName, ResponseBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Unable to post to {StatsSiteName}", SiteName);
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        public abstract bool SetAuthorization(HttpClient httpClient, string apiKey);
    }
}
