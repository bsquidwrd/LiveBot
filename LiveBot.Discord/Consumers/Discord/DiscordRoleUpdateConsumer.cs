using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using MassTransit;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordRoleUpdateConsumer : IConsumer<IDiscordRoleUpdate>
    {
        private readonly IUnitOfWork _work;

        public DiscordRoleUpdateConsumer(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IDiscordRoleUpdate> context)
        {
            var message = context.Message;
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == message.GuildId);
            var discordRole = new DiscordRole
            {
                DiscordGuild = discordGuild,
                DiscordId = message.RoleId,
                Name = message.RoleName
            };
            await _work.RoleRepository.AddOrUpdateAsync(discordRole, i => i.DiscordGuild == discordGuild && i.DiscordId == message.RoleId);
        }
    }
}