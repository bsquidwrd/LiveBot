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

            var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == message.GuildId);
            if (guildConfig != null && guildConfig?.DiscordChannel == channel)
            {
                guildConfig.DiscordChannel = null;
                await _work.GuildConfigRepository.UpdateAsync(guildConfig);
            }

            var roleSubscriptions = await _work.SubscriptionRepository.FindAsync(i =>
                i.DiscordGuild.DiscordId == message.GuildId
                && i.DiscordGuild.Config.DiscordChannel.DiscordId == message.ChannelId
                && i.IsFromRole == true
            );

            foreach (var roleSubscription in roleSubscriptions)
            {
                await _work.SubscriptionRepository.RemoveAsync(roleSubscription.Id);
            }

            await _work.ChannelRepository.RemoveAsync(channel.Id);
        }
    }
}