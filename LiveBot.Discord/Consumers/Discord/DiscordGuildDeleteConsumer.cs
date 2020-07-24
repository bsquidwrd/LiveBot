using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;
using Serilog;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordGuildDeleteConsumer : IConsumer<IDiscordGuildDelete>
    {
        private readonly IUnitOfWork _work;

        public DiscordGuildDeleteConsumer(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IDiscordGuildDelete> context)
        {
            var message = context.Message;
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == message.GuildId);

            if (discordGuild == null)
                return;

            var discordChannels = discordGuild.DiscordChannels;
            var discordRoles = discordGuild.DiscordRoles;
            var streamSubscriptions = discordGuild.StreamSubscriptions;

            try
            {
                // Remove Discord Guild Config
                if (discordGuild.Config != null)
                    await _work.GuildConfigRepository.RemoveAsync(discordGuild.Config.Id);

                // Remove Stream Subscriptions for this Guild
                foreach (var streamSubscription in streamSubscriptions)
                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);

                // Remove Discord Roles for this Guild
                foreach (var discordRole in discordRoles)
                    await _work.RoleRepository.RemoveAsync(discordRole.Id);

                // Remove Discord Channels for this Guild
                foreach (var discordChannel in discordChannels)
                    await _work.ChannelRepository.RemoveAsync(discordChannel.Id);

                // Remove Discord Guild
                await _work.GuildRepository.RemoveAsync(discordGuild.Id);
            }
            catch
            {
                Log.Error($"Unable to remove Guild {discordGuild.DiscordId} {discordGuild.Name}");
            }
        }
    }
}