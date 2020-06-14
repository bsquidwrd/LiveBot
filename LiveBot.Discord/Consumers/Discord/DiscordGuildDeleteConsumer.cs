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

            var discordChannels = await _work.ChannelRepository.FindAsync(d => d.DiscordGuild == discordGuild);
            var discordRoles = await _work.RoleRepository.FindAsync(d => d.DiscordGuild == discordGuild);
            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(d => d.DiscordChannel.DiscordGuild == discordGuild);

            foreach (var streamSubscription in streamSubscriptions)
            {
                try
                {
                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
                }
                catch
                {
                    Log.Error($"Unable to remove Stream Subscription {streamSubscription.User.SourceID} {streamSubscription.User.Username} in {discordGuild.DiscordId} {discordGuild.Name}");
                    continue;
                }
            }

            foreach (var discordChannel in discordChannels)
            {
                try
                {
                    await _work.ChannelRepository.RemoveAsync(discordChannel.Id);
                }
                catch
                {
                    Log.Error($"Unable to remove Channel {discordChannel.DiscordId} {discordChannel.Name}");
                    continue;
                }
            }

            foreach (var discordRole in discordRoles)
            {
                try
                {
                    await _work.RoleRepository.RemoveAsync(discordRole.Id);
                }
                catch
                {
                    Log.Error($"Unable to remove Role {discordRole.DiscordId} {discordRole.Name}");
                    continue;
                }
            }

            try
            {
                await _work.GuildRepository.RemoveAsync(discordGuild.Id);
            }
            catch
            {
                Log.Error($"Unable to remove Guild {discordGuild.DiscordId} {discordGuild.Name}");
            }
        }
    }
}