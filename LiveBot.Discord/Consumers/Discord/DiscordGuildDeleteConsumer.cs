using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;
using Serilog;
using System.Linq;
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
                // Remove Stream Subscriptions for this Guild
                streamSubscriptions.ToList().ForEach(async i => await _work.SubscriptionRepository.RemoveAsync(i.Id));

                // Remove Discord Roles for this Guild
                discordRoles.ToList().ForEach(async i => await _work.RoleRepository.RemoveAsync(i.Id));

                // Remove Discord Channels for this Guild
                discordChannels.ToList().ForEach(async i => await _work.ChannelRepository.RemoveAsync(i.Id));

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