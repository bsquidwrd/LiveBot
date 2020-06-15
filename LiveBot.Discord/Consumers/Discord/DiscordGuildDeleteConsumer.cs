using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Discord.Contracts;
using MassTransit;
using Serilog;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordGuildDeleteConsumer : IConsumer<IDiscordGuildDelete>
    {
        private readonly IUnitOfWork _work;
        private readonly IBusControl _bus;

        public DiscordGuildDeleteConsumer(IUnitOfWorkFactory factory, IBusControl bus)
        {
            _work = factory.Create();
            _bus = bus;
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
                    var channelContext = new DiscordChannelDelete { GuildId = discordGuild.DiscordId, ChannelId = discordChannel.DiscordId };
                    await _bus.Publish(channelContext);
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
                    var roleContext = new DiscordRoleDelete { GuildId = discordGuild.DiscordId, RoleId = discordRole.DiscordId };
                    await _bus.Publish(roleContext);
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