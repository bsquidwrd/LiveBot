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
            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(d => d.DiscordId == message.GuildId);

            if (discordGuild == null)
                discordGuild = new DiscordGuild();

            discordGuild.Name = message.GuildName;
            discordGuild.IconUrl = message.IconUrl;
            discordGuild.IsInBeta = discordGuild.IsInBeta;

            await _work.GuildRepository.AddOrUpdateAsync(discordGuild, (d => d.DiscordId == message.GuildId));
        }
    }
}