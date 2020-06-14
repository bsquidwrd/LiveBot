using Discord.WebSocket;
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
        private readonly DiscordShardedClient _client;

        public DiscordGuildUpdateConsumer(IUnitOfWorkFactory factory, DiscordShardedClient client)
        {
            _work = factory.Create();
            _client = client;
        }

        public async Task Consume(ConsumeContext<IDiscordGuildUpdate> context)
        {
            var message = context.Message;
            var guild = _client.GetGuild(message.GuildId);
            var shard = _client.GetShardFor(guild);

            if (guild == null || shard == null)
                return;

            DiscordGuild discordGuild = new DiscordGuild() { DiscordId = guild.Id, Name = guild.Name };
            await _work.GuildRepository.AddOrUpdateAsync(discordGuild, (d => d.DiscordId == message.GuildId));
        }
    }
}