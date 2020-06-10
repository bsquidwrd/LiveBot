using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;
using Serilog;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers
{
    public class StreamUpdateConsumer : IConsumer<IStreamUpdate>
    {
        private readonly DiscordShardedClient _client;
        private readonly IUnitOfWork _work;

        public StreamUpdateConsumer(DiscordShardedClient client, IUnitOfWorkFactory factory)
        {
            _client = client;
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IStreamUpdate> context)
        {
            Log.Debug("Consume for IStreamUpdate called");
            await Task.Delay(1);
        }
    }
}