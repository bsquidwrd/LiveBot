using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Discord.Contracts;
using MassTransit;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordAlertConsumer : IConsumer<IDiscordAlert>
    {
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;

        public DiscordAlertConsumer(IUnitOfWorkFactory factory, IBusControl bus)
        {
            _work = factory.Create();
            _bus = bus;
        }

        public async Task Consume(ConsumeContext<IDiscordAlert> context)
        {
            var Message = context.Message;
            var discordSubscriptionChannels = await _work.SubscriptionRepository.GetAllAsync();
            var distinctDiscordChannels = discordSubscriptionChannels.Select(i => i.DiscordChannel).Distinct();

            if (distinctDiscordChannels.Count() == 0)
                return;

            foreach (var discordChannel in distinctDiscordChannels)
            {
                var alertContext = new DiscordAlertChannel { Message = Message.Message, ChannelId = discordChannel.DiscordId };
                await _bus.Publish(alertContext);
            }
        }
    }
}