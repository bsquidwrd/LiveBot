using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers
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
            await Task.CompletedTask;
        }
    }
}