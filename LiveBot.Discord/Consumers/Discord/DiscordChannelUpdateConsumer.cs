using Discord.WebSocket;
using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using MassTransit;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordChannelUpdateConsumer : IConsumer<IDiscordChannelUpdate>
    {
        private readonly IUnitOfWork _work;
        private readonly DiscordShardedClient _client;

        public DiscordChannelUpdateConsumer(IUnitOfWorkFactory factory, DiscordShardedClient client)
        {
            _work = factory.Create();
            _client = client;
        }

        public async Task Consume(ConsumeContext<IDiscordChannelUpdate> context)
        {
            var message = context.Message;
            var guild = _client.GetGuild(message.GuildId);

            if (guild == null)
                return;

            var channel = guild.GetTextChannel(message.ChannelId);

            if (channel == null)
                return;

            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == message.GuildId);
            var discordChannel = new DiscordChannel
            {
                DiscordGuild = discordGuild,
                DiscordId = channel.Id,
                Name = channel.Name
            };
            await _work.ChannelRepository.AddOrUpdateAsync(discordChannel, i => i.DiscordGuild == discordGuild && i.DiscordId == channel.Id);
        }
    }
}