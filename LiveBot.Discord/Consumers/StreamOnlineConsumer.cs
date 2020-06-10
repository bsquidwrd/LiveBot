using Discord;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.Helpers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers
{
    public class StreamOnlineConsumer : IConsumer<IStreamOnline>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;

        public StreamOnlineConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory)
        {
            _client = client;
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IStreamOnline> context)
        {
            ILiveBotStream stream = context.Message.Stream;
            var streamUser = await _work.StreamUserRepository.SingleOrDefaultAsync(i => i.ServiceType == stream.ServiceType && i.SourceID == stream.User.Id);
            var streamSubscriptions = await _work.StreamSubscriptionRepository.FindAsync(i => i.User == streamUser);

            foreach (var streamSubscription in streamSubscriptions)
            {
                SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(streamSubscription.DiscordChannel.DiscordId);
                string notificationMessage = NotificationHelpers.GetNotificationMessage(stream, streamSubscription);
                Embed embed = NotificationHelpers.GetStreamEmbed(stream);

                await channel.SendMessageAsync(text: notificationMessage, embed: embed);
            }
        }
    }
}