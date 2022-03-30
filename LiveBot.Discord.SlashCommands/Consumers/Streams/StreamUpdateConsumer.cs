using Discord.Rest;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    public class StreamUpdateConsumer : IConsumer<IStreamUpdate>
    {
        private readonly DiscordRestClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;
        private readonly IEnumerable<ILiveBotMonitor> _monitors;
        private readonly ILogger<StreamUpdateConsumer> _logger;

        public StreamUpdateConsumer(DiscordRestClient client, IUnitOfWorkFactory factory, IBusControl bus, IEnumerable<ILiveBotMonitor> monitors, ILogger<StreamUpdateConsumer> logger)
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

            ILiveBotUser user = stream.User ?? await monitor.GetUserById(stream.UserId);
            ILiveBotGame game = stream.Game ?? await monitor.GetGame(stream.GameId);

            Expression<Func<StreamGame, bool>> templateGamePredicate = (i => i.ServiceType == stream.ServiceType && i.SourceId == "0");
            var templateGame = await _work.GameRepository.SingleOrDefaultAsync(templateGamePredicate);
            var streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == user.Id);
            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);

            StreamGame streamGame;
            if (game.Id == "0" || string.IsNullOrEmpty(game.Id))
            {
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
                    templateGame = await _work.GameRepository.SingleOrDefaultAsync(templateGamePredicate);
                }
                streamGame = templateGame;
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
                streamGame = await _work.GameRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceId == stream.GameId);
            }

            if (!streamSubscriptions.Any())
                return;

            List<StreamSubscription> unsentSubscriptions = new List<StreamSubscription>();

            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                if (streamSubscription.DiscordGuild == null || streamSubscription.DiscordChannel == null)
                {
                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
                    continue;
                }

                var discordChannel = streamSubscription.DiscordChannel;
                var discordRole = streamSubscription.DiscordRole;
                var discordGuild = streamSubscription.DiscordGuild;

                Expression<Func<StreamNotification, bool>> previousNotificationPredicate = (i =>
                    i.User_SourceID == streamUser.SourceID &&
                    i.DiscordGuild_DiscordId == discordGuild.DiscordId &&
                    i.DiscordChannel_DiscordId == discordChannel.DiscordId &&
                    i.Stream_StartTime == stream.StartTime &&
                    i.Stream_SourceID == stream.Id &&
                    i.Success == true
                );

                var previousNotifications = await _work.NotificationRepository.FindAsync(previousNotificationPredicate);
                if (previousNotifications.Any())
                    continue;

                unsentSubscriptions.Add(streamSubscription);
            }

            if (unsentSubscriptions.Count > 0)
            {
                await _bus.Publish<IStreamOnline>(new { Stream = stream });
            }
        }
    }
}