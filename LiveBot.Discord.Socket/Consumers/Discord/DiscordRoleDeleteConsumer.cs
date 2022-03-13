using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;

namespace LiveBot.Discord.Socket.Consumers.Discord
{
    public class DiscordRoleDeleteConsumer : IConsumer<IDiscordRoleDelete>
    {
        private readonly IUnitOfWork _work;

        public DiscordRoleDeleteConsumer(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IDiscordRoleDelete> context)
        {
            var message = context.Message;
            var role = await _work.RoleRepository.SingleOrDefaultAsync(i => i.DiscordId == message.RoleId && i.DiscordGuild.DiscordId == message.GuildId);

            if (role == null)
                return;

            var subscriptions = await _work.SubscriptionRepository.FindAsync(i => i.DiscordRole.DiscordId == message.RoleId && i.DiscordGuild.DiscordId == message.GuildId);
            foreach (var subscription in subscriptions)
            {
                subscription.DiscordRole = null;
                await _work.SubscriptionRepository.UpdateAsync(subscription);
            }

            var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == message.GuildId);
            if (guildConfig != null)
            {
                bool update = false;
                if (guildConfig.DiscordRole == role)
                {
                    guildConfig.DiscordRole = null;
                    update = true;
                }
                if (guildConfig.MonitorRole == role)
                {
                    guildConfig.MonitorRole = null;
                    update = true;
                }

                if (update)
                    await _work.GuildConfigRepository.UpdateAsync(guildConfig);
            }

            await _work.RoleRepository.RemoveAsync(role.Id);
        }
    }
}