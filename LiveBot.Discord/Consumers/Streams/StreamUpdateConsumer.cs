using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Streams
{
    public class StreamUpdateConsumer : IConsumer<IStreamUpdate>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;

        public StreamUpdateConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory, IBusControl bus)
        {
            _client = client;
            _work = factory.Create();
            _bus = bus;
        }

        public async Task Consume(ConsumeContext<IStreamUpdate> context)
        {
            ILiveBotStream stream = context.Message.Stream;
            var streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == stream.UserId);
            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);

            if (streamSubscriptions.Count() == 0)
                return;

            List<StreamSubscription> unsentSubscriptions = new List<StreamSubscription>();

            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                var discordGuild = streamSubscription.DiscordGuild;
                var discordChannel = streamSubscription.DiscordChannel;

                if (discordGuild == null || discordChannel == null)
                {
                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
                    return;
                }

                Expression<Func<StreamNotification, bool>> previousNotificationPredicate = (i =>
                    i.User_SourceID == streamUser.SourceID &&
                    i.DiscordGuild_DiscordId == discordGuild.DiscordId
                );

                var previousStreamNotifications = await _work.NotificationRepository.FindAsync(previousNotificationPredicate);

                var previousNotifications = previousStreamNotifications.Where(i =>
                    i.DiscordGuild_DiscordId == discordGuild.DiscordId &&
                    i.Stream_SourceID == stream.Id &&
                    i.Stream_StartTime == stream.StartTime &&
                    i.Success == true
                );

                if (previousNotifications.Count() > 0)
                    continue;

                unsentSubscriptions.Add(streamSubscription);
            }

            if (unsentSubscriptions.Count() > 0)
            {
                await _bus.Publish<IStreamOnline>(new { Stream = stream });
            }
        }
    }
}