using System.Linq.Expressions;
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

            Expression<Func<DiscordGuild, bool>> predicate = (i =>
                    i.DiscordId == message.GuildId
                );

            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(predicate);

            if (discordGuild == null)
            {
                var newGuild = new DiscordGuild
                {
                    DiscordId = message.GuildId,
                    Name = message.GuildName,
                    IconUrl = message.IconUrl,
                    IsInBeta = false
                };
                await _work.GuildRepository.AddAsync(newGuild);
                discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(predicate);
            }

            discordGuild.Name = message.GuildName;
            discordGuild.IconUrl = message.IconUrl;

            await _work.GuildRepository.UpdateAsync(discordGuild);
        }
    }
}