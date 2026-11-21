using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    public class StreamUpdateConsumer : IConsumer<IStreamUpdate>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBus _bus;
        private readonly IEnumerable<ILiveBotMonitor> _monitors;
        private readonly ILogger<StreamUpdateConsumer> _logger;

        public StreamUpdateConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory, IBus bus, IEnumerable<ILiveBotMonitor> monitors, ILogger<StreamUpdateConsumer> logger)
        {
            _client = client;
            _work = factory.Create();
            _bus = bus;
            _monitors = monitors;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IStreamUpdate> context)
        {
            ILiveBotStream stream = context.Message.Stream;
            ILiveBotUser? user = stream.User;

            // Monitors are only registered here when RunWatchersInDiscord is true.
            if (user == null)
            {
                ILiveBotMonitor? monitor = _monitors.FirstOrDefault(i => i.ServiceType == stream.ServiceType);
                if (monitor != null)
                    user = await monitor.GetUserById(stream.UserId);
            }

            if (user == null)
            {
                _logger.LogWarning("Skipping stream update for {UserId} on {ServiceType}: message has no user and no monitor could resolve one", stream.UserId, stream.ServiceType);
                return;
            }

            var streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == user.Id);
            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);

            if (!streamSubscriptions.Any())
                return;

            bool hasValidSubscriptions = false;

            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                if (streamSubscription.DiscordGuild == null || streamSubscription.DiscordChannel == null)
                {
                    _logger.LogInformation("Removing orphaned Stream Subscription for {Username} on {ServiceType} due to Guild or Channel not found in database - {SubscriptionId}", streamUser.Username, stream.ServiceType, streamSubscription.Id);
                    await _work.SubscriptionRepository.RemoveWithRolesAsync(streamSubscription.Id);
                    continue;
                }

                hasValidSubscriptions = true;
            }

            if (hasValidSubscriptions)
            {
                await _bus.Publish<IStreamOnline>(new { Stream = stream });
            }
        }
    }
}