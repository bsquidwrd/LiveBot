using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordChannelDeleteConsumer : IConsumer<IDiscordChannelDelete>
    {
        private readonly IUnitOfWork _work;

        public DiscordChannelDeleteConsumer(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IDiscordChannelDelete> context)
        {
            var message = context.Message;
            var channel = await _work.ChannelRepository.SingleOrDefaultAsync(i => i.DiscordId == message.ChannelId && i.DiscordGuild.DiscordId == message.GuildId);

            if (channel == null)
                return;

            var subscriptions = await _work.SubscriptionRepository.FindAsync(i => i.DiscordChannel.DiscordId == message.ChannelId && i.DiscordGuild.DiscordId == message.GuildId);
            foreach (var subscription in subscriptions)
            {
                await _work.SubscriptionRepository.RemoveAsync(subscription.Id);
            }

            await _work.ChannelRepository.RemoveAsync(channel.Id);
        }
    }
}