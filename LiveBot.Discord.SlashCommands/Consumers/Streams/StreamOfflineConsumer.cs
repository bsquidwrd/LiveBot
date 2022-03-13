using Discord.Rest;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;

namespace LiveBot.Discord.SlashCommands.Consumers.Streams
{
    public class StreamOfflineConsumer : IConsumer<IStreamOffline>
    {
        private readonly DiscordRestClient _client;
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;

        public StreamOfflineConsumer(DiscordRestClient client, IUnitOfWorkFactory factory, IBusControl bus)
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