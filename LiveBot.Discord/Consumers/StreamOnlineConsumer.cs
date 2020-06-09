using Discord;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.Helpers;
using MassTransit;
using Serilog;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers
{
    public class StreamOnlineConsumer : IConsumer<IStreamOnline>
    {
        private readonly LiveBotDiscord _client;

        public StreamOnlineConsumer(IDiscordClient client)
        {
            _client = (LiveBotDiscord)client;
        }

        public async Task Consume(ConsumeContext<IStreamOnline> context)
        {
            Log.Debug($"{_client is LiveBotDiscord}");
            await Task.Delay(1);
            //StreamSubscription streamSubscription = context.Message.Subscription;
            //ILiveBotStream stream = context.Message.Stream;
            //SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(streamSubscription.DiscordChannel.DiscordId);
            //string notificationMessage = NotificationHelpers.GetNotificationMessage(stream, streamSubscription);
            //Embed embed = NotificationHelpers.GetStreamEmbed(stream);
            //return channel.SendMessageAsync(text: notificationMessage, embed: embed);
        }
    }
}