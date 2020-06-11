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

        public StreamOfflineConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory)
        {
            _client = client;
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IStreamOffline> context)
        {
            // TODO: Implement StreamOffline.Consume
            await Task.CompletedTask;
        }
    }
}