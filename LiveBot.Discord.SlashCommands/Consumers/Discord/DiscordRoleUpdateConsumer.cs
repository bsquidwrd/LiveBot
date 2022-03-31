using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using MassTransit;
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Consumers.Discord
{
    public class DiscordRoleUpdateConsumer : IConsumer<IDiscordRoleUpdate>
    {
        private readonly IUnitOfWork _work;
        private readonly ILogger<DiscordRoleUpdateConsumer> _logger;
        private readonly IBusControl _bus;

        public DiscordRoleUpdateConsumer(IUnitOfWorkFactory factory, ILogger<DiscordRoleUpdateConsumer> logger, IBusControl bus)
        {
            _work = factory.Create();
            _logger = logger;
            _bus = bus;
        }

        public async Task Consume(ConsumeContext<IDiscordRoleUpdate> context)
        {
            var message = context.Message;
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == message.GuildId);
            if (discordGuild == null)
                return;

            Expression<Func<DiscordRole, bool>> predicate = (i =>
                    i.DiscordGuild.DiscordId == message.GuildId &&
                    i.DiscordId == message.RoleId
                );

            var discordRole = await _work.RoleRepository.SingleOrDefaultAsync(predicate);

            if (discordRole == null)
            {
                var newRole = new DiscordRole
                {
                    DiscordGuild = discordGuild,
                    DiscordId = message.RoleId,
                    Name = message.RoleName
                };
                await _work.RoleRepository.AddAsync(newRole);
                discordRole = await _work.RoleRepository.SingleOrDefaultAsync(predicate);
            }

            discordRole.Name = message.RoleName;

            await _work.RoleRepository.UpdateAsync(discordRole);
        }
    }
}