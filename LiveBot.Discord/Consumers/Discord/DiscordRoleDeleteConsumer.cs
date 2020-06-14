using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
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

            await _work.RoleRepository.RemoveAsync(role.Id);
        }
    }
}