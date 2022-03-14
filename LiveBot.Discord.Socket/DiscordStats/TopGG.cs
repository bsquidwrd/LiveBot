using Discord;
using DiscordBotsList.Api;
using LiveBot.Core.Repository.Interfaces.Discord;
using System.Timers;

namespace LiveBot.Discord.Socket.DiscordStats
{
    public class TopGG : IDiscordStats
    {
        private readonly ILogger<TopGG> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDiscordClient _discordClient;
        private System.Timers.Timer? _timer = null;
        private AuthDiscordBotListApi? _api = null;
        private readonly bool IsDebug = false;

        public TopGG(ILogger<TopGG> logger, IConfiguration configuration, IDiscordClient discordClient)
        {
            _logger = logger;
            _configuration = configuration;
            _discordClient = discordClient;

            IsDebug = _configuration.GetValue<bool>("IsDebug");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!IsDebug)
            {
                _api = new AuthDiscordBotListApi(_discordClient.CurrentUser.Id, _configuration.GetValue<string>("TopGG_API"));
                TimeSpan timeSpan = TimeSpan.FromMinutes(15);
                var timer = new System.Timers.Timer(timeSpan.TotalMilliseconds)
                {
                    AutoReset = true
                };
                timer.Elapsed += UpdateStats;
                _timer = timer;
                _timer.Start();
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            _timer = null;
            return Task.CompletedTask;
        }

        public async void UpdateStats(object? sender = null, ElapsedEventArgs? e = null)
        {
            if (_api == null || IsDebug)
                return;
            var guilds = await _discordClient.GetGuildsAsync();
            var me = await _api.GetMeAsync();
            await me.UpdateStatsAsync(guildCount: guilds.Count);
        }
    }
}