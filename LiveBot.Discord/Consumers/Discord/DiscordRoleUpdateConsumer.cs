using Discord.WebSocket;
using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using MassTransit;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordRoleUpdateConsumer : IConsumer<IDiscordRoleUpdate>
    {
        private readonly IUnitOfWork _work;
        private readonly DiscordShardedClient _client;
        public DiscordRoleUpdateConsumer(IUnitOfWorkFactory factory, DiscordShardedClient client)
        {
            _work = factory.Create();
            _client = client;
        }

        public async Task Consume(ConsumeContext<IDiscordRoleUpdate> context)
        {
            var message = context.Message;
            var guild = _client.GetGuild(message.GuildId);
            var shard = _client.GetShardFor(guild);

            if (guild == null || shard == null)
                return;

            var role = guild.GetRole(message.RoleId);

            if (role == null)
                return;

            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == message.GuildId);
            var discordRole = new DiscordRole
            {
                DiscordGuild = discordGuild,
                DiscordId = role.Id,
                Name = role.Name
            };
            await _work.RoleRepository.AddOrUpdateAsync(discordRole, i => i.DiscordGuild == discordGuild && i.DiscordId == role.Id);
        }
    }
}