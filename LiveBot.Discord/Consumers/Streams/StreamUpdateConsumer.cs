using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using MassTransit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
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
            // TODO: Implement StreamOUpdate.Consume
            // Also find a way to check if a notification has been sent out, but not for the existing subscriptions
            // If so, send it out.
            // Because users are monitored on load, if someone gets a subscription setup after someone is live and they didn't exist before
            // it won't notify because they are being "updated"
            try
            {
                ILiveBotStream stream = context.Message.Stream;
                var streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == stream.UserId);
                var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.User == streamUser);

                if (streamSubscriptions.Count() == 0)
                    return;

                List<StreamSubscription> unsentSubscriptions = new List<StreamSubscription>();

                foreach (StreamSubscription streamSubscription in streamSubscriptions)
                {
                    var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i == streamSubscription.DiscordGuild);
                    var discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(i => i == streamSubscription.DiscordChannel);

                    var previousNotifications = await _work.NotificationRepository.FindAsync(i =>
                        i.ServiceType == stream.ServiceType &&
                        i.User_SourceID == streamUser.SourceID &&
                        i.DiscordGuild_DiscordId == discordGuild.DiscordId &&
                        i.DiscordChannel_DiscordId == discordChannel.DiscordId &&
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
            catch (Exception e)
            {
                Log.Error($"{e}");
            }
        }
    }
}