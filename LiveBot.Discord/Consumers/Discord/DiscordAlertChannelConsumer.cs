using Discord.WebSocket;
using LiveBot.Core.Contracts.Discord;
using MassTransit;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordAlertChannelConsumer : IConsumer<IDiscordAlertChannel>
    {
        private readonly DiscordShardedClient _client;

        public DiscordAlertChannelConsumer(DiscordShardedClient client)
        {
            _client = client;
        }

        public async Task Consume(ConsumeContext<IDiscordAlertChannel> context)
        {
            var Message = context.Message;
            var channel = (SocketTextChannel)_client.GetChannel(Message.ChannelId);
            await channel.SendMessageAsync($"{Message.Message}"); ;
        }
    }
}