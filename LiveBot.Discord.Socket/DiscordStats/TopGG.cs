using Discord;
using Discord.WebSocket;
using DiscordBotsList.Api;
using LiveBot.Core.Interfaces.Discord;

namespace LiveBot.Discord.Socket.DiscordStats
{
    public class TopGG : IDiscordStats
    {
        private readonly ILogger<TopGG> _logger;
        private readonly IConfiguration _configuration;
        private readonly DiscordShardedClient _discordClient;
        private System.Timers.Timer? _timer = null;
        private AuthDiscordBotListApi? _api = null;
        private readonly bool IsDebug = false;

        public TopGG(ILogger<TopGG> logger, IConfiguration configuration, DiscordShardedClient discordClient)
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
            if (_api == null)
                _api = new AuthDiscordBotListApi(_discordClient.CurrentUser.Id, _configuration.GetValue<string>("TopGG_API"));
            var guilds = _discordClient.Guilds;
            var me = await _api.GetMeAsync();
            await me.UpdateStatsAsync(guildCount: guilds.Count);
            _logger.LogInformation(message: "Updated Guild Count for Top.GG: {GuildCount}", guilds.Count);
        }
    }
}