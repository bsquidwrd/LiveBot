using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using MassTransit;

namespace LiveBot.Discord.SlashCommands.Consumers.Discord
{
    public class DiscordGuildUpdateConsumer : IConsumer<IDiscordGuildUpdate>
    {
        private readonly IUnitOfWork _work;
        private readonly ILogger<DiscordGuildUpdateConsumer> _logger;

        public DiscordGuildUpdateConsumer(IUnitOfWorkFactory factory, ILogger<DiscordGuildUpdateConsumer> logger)
        {
            _work = factory.Create();
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IDiscordGuildUpdate> context)
        {
            var message = context.Message;
            var existingDiscordGuild = await _work.GuildRepository.SingleOrDefaultAsync(d => d.DiscordId == message.GuildId);

            var discordGuild = new DiscordGuild
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