using System.Timers;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;

namespace LiveBot.Discord.SlashCommands.DiscordStats
{
    public class LiveBotStats : IHostedService
    {
        internal readonly ILogger<LiveBotStats> _logger;
        internal readonly DiscordShardedClient _discordClient;
        internal readonly IUnitOfWork _work;
        internal System.Timers.Timer _timer;

        public LiveBotStats(ILogger<LiveBotStats> logger, DiscordShardedClient discordClient, IUnitOfWorkFactory factory)
        {
            _logger = logger;
            _discordClient = discordClient;
            _work = factory.Create();

            TimeSpan timeSpan = TimeSpan.FromMinutes(5);
            _timer = new System.Timers.Timer(timeSpan.Duration().TotalMilliseconds)
            {
                AutoReset = true
            };
            _timer.Elapsed += LogInformation;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Stop();
            return Task.CompletedTask;
        }

        public async void LogInformation(object? sender = null, ElapsedEventArgs? e = null)
        {
            _logger.LogInformation(
                "Current Stats: {GuildCount} {SubscriptionCount}",
                _discordClient.Guilds.Count,
                await _work.SubscriptionRepository.LongCountAsync()
            );
        }
    }
}