using Discord;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.Helpers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers
{
    public class StreamOnlineConsumer : IConsumer<IStreamOnline>
    {
        //private readonly IDiscordClient _client;
        //public StreamOnlineConsumer(IDiscordClient client)
        //{
        //    _client = client;
        //}
        private readonly IServiceProvider _services;
        public StreamOnlineConsumer(IServiceProvider services)
        {
            _services = services;
        }

        public async Task Consume(ConsumeContext<IStreamOnline> context)
        {
            DiscordShardedClient _client = _services.GetRequiredService<DiscordShardedClient>();
            StreamSubscription streamSubscription = context.Message.Subscription;

            ILiveBotStream stream = context.Message.Stream;
            SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(streamSubscription.DiscordChannel.DiscordId);

            string notificationMessage = NotificationHelpers.GetNotificationMessage(stream, streamSubscription);
            Embed embed = NotificationHelpers.GetStreamEmbed(stream);

            await channel.SendMessageAsync(text: notificationMessage, embed: embed);
        }
    }
}