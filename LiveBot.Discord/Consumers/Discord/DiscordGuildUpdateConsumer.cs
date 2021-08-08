using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using MassTransit;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordGuildUpdateConsumer : IConsumer<IDiscordGuildUpdate>
    {
        private readonly IUnitOfWork _work;

        public DiscordGuildUpdateConsumer(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IDiscordGuildUpdate> context)
        {
            var message = context.Message;
            DiscordGuild existingDiscordGuild = await _work.GuildRepository.SingleOrDefaultAsync(d => d.DiscordId == message.GuildId);

            DiscordGuild discordGuild = new DiscordGuild
            {
                DiscordId = message.GuildId,
                Name = message.GuildName,
                IconUrl = message.IconUrl,
                IsInBeta = existingDiscordGuild?.IsInBeta ?? false
            };

            await _work.GuildRepository.AddOrUpdateAsync(discordGuild, (d => d.DiscordId == message.GuildId));
        }
    }
}