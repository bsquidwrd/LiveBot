using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers
{
    public class StreamOfflineConsumer : IConsumer<IStreamOffline>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;

        public StreamOfflineConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory, IBusControl bus)
        {
            _client = client;
            _work = factory.Create();
            _bus = bus;
        }

        public async Task Consume(ConsumeContext<IStreamOffline> context)
        {
            // TODO: Implement StreamOffline.Consume
            await Task.CompletedTask;
        }
    }
}