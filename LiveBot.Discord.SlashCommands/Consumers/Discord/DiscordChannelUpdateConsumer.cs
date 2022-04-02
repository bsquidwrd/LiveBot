using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using MassTransit;
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Consumers.Discord
{
    public class DiscordChannelUpdateConsumer : IConsumer<IDiscordChannelUpdate>
    {
        private readonly IUnitOfWork _work;
        private readonly ILogger<DiscordChannelUpdateConsumer> _logger;

        public DiscordChannelUpdateConsumer(IUnitOfWorkFactory factory, ILogger<DiscordChannelUpdateConsumer> logger)
        {
            _work = factory.Create();
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IDiscordChannelUpdate> context)
        {
            var message = context.Message;
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == message.GuildId);
            if (discordGuild == null)
                return;

            Expression<Func<DiscordChannel, bool>> predicate = (i =>
                    i.DiscordId == message.ChannelId
                );

            var discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(predicate);

            if (discordChannel == null)
            {
                var newChannel = new DiscordChannel
                {
                    DiscordGuild = discordGuild,
                    DiscordId = message.ChannelId,
                    Name = message.ChannelName
                };
                await _work.ChannelRepository.AddAsync(newChannel);
                discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(predicate);
            }

            discordChannel.DiscordGuild = discordGuild;
            discordChannel.Name = message.ChannelName;

            await _work.ChannelRepository.UpdateAsync(discordChannel);
        }
    }
}