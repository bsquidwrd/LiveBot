using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;

namespace LiveBot.Discord.Socket.Consumers.Discord
{
    public class DiscordGuildDeleteConsumer : IConsumer<IDiscordGuildDelete>
    {
        private readonly ILogger<DiscordGuildDeleteConsumer> _logger;
        private readonly IUnitOfWork _work;

        public DiscordGuildDeleteConsumer(ILogger<DiscordGuildDeleteConsumer> logger, IUnitOfWorkFactory factory)
        {
            _logger = logger;
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IDiscordGuildDelete> context)
        {
            try
            {
                var message = context.Message;
                var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == message.GuildId);

                if (discordGuild == null)
                    return;

                var discordChannels = await _work.ChannelRepository.FindAsync(i => i.DiscordGuild.DiscordId == message.GuildId);
                var discordRoles = await _work.RoleRepository.FindAsync(i => i.DiscordGuild.DiscordId == message.GuildId);
                var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(i => i.DiscordGuild.DiscordId == message.GuildId);

                // Remove Discord Guild Config
                if (discordGuild.Config != null)
                    await _work.GuildConfigRepository.RemoveAsync(discordGuild.Config.Id);

                // Remove Stream Subscriptions for this Guild
                foreach (var streamSubscription in streamSubscriptions.ToList())
                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);

                // Remove Discord Roles for this Guild
                foreach (var discordRole in discordRoles.ToList())
                    await _work.RoleRepository.RemoveAsync(discordRole.Id);

                // Remove Discord Channels for this Guild
                foreach (var discordChannel in discordChannels.ToList())
                    await _work.ChannelRepository.RemoveAsync(discordChannel.Id);

                // Remove Discord Guild
                await _work.GuildRepository.RemoveAsync(discordGuild.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Unable to remove Guild {guildId}", context.Message.GuildId);
            }
        }
    }
}