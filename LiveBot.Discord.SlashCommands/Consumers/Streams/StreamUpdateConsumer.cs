using System.Linq.Expressions;
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
            ILiveBotMonitor? monitor = _monitors.Where(i => i.ServiceType == stream.ServiceType).FirstOrDefault();

            if (monitor == null)
                return;

            ILiveBotUser? user = stream.User ?? await monitor.GetUserById(stream.UserId);
            if (user == null)
            {
                _logger.LogWarning("Could not resolve user for stream {StreamId} on {ServiceType} during stream update; skipping",
                    stream.Id, stream.ServiceType);
                return;
            }

            ILiveBotGame game = stream.Game ?? await monitor.GetGame(stream.GameId);

            var streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == user.Id);

            if (streamUser == null)
            {
                _logger.LogWarning("StreamUser not found for {UserId} on {ServiceType} during stream update; skipping",
                    user.Id, stream.ServiceType);
                return;
            }

            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);

            if (!streamSubscriptions.Any())
                return;

            // Ensure the game record exists in the database
            Expression<Func<StreamGame, bool>> templateGamePredicate = (i => i.ServiceType == stream.ServiceType && i.SourceId == "0");
            if (game.Id == "0" || string.IsNullOrEmpty(game.Id))
            {
                var templateGame = await _work.GameRepository.SingleOrDefaultAsync(templateGamePredicate);
                if (templateGame == null)
                {
                    StreamGame newStreamGame = new StreamGame
                    {
                        ServiceType = stream.ServiceType,
                        SourceId = "0",
                        Name = "[Not Set]",
                        ThumbnailURL = ""
                    };
                    await _work.GameRepository.AddOrUpdateAsync(newStreamGame, templateGamePredicate);
                }
            }
            else
            {
                StreamGame newStreamGame = new StreamGame
                {
                    ServiceType = stream.ServiceType,
                    SourceId = game.Id,
                    Name = game.Name,
                    ThumbnailURL = game.ThumbnailURL
                };
                await _work.GameRepository.AddOrUpdateAsync(newStreamGame, i => i.ServiceType == stream.ServiceType && i.SourceId == stream.GameId);
            }

            bool hasValidSubscriptions = false;

            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                if (streamSubscription.DiscordGuild == null || streamSubscription.DiscordChannel == null)
                {
                    _logger.LogInformation("Removing orphaned Stream Subscription for {Username} on {ServiceType} due to Guild or Channel not found in database - {SubscriptionId}", streamUser.Username, stream.ServiceType, streamSubscription.Id);
                    var rolesToMention = await _work.RoleToMentionRepository.FindAsync(i => i.StreamSubscription == streamSubscription);
                    foreach (var roleToMention in rolesToMention)
                        await _work.RoleToMentionRepository.RemoveAsync(roleToMention.Id);
                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
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