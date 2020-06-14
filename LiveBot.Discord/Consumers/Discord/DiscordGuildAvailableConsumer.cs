using Discord;
using Discord.WebSocket;
using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Discord.Contracts;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Discord.Consumers.Discord
{
    public class DiscordGuildAvailableConsumer : IConsumer<IDiscordGuildAvailable>
    {
        private readonly IUnitOfWork _work;
        private readonly DiscordShardedClient _client;
        private readonly IBusControl _bus;

        public DiscordGuildAvailableConsumer(IUnitOfWorkFactory factory, DiscordShardedClient client, IBusControl bus)
        {
            _work = factory.Create();
            _client = client;
            _bus = bus;
        }

        public async Task Consume(ConsumeContext<IDiscordGuildAvailable> context)
        {
            try
            {
                var message = context.Message;
                var guild = _client.GetGuild(message.GuildId);
                var shard = _client.GetShardFor(guild);

                DiscordGuild discordGuild = new DiscordGuild() { DiscordId = guild.Id, Name = guild.Name };
                await _work.GuildRepository.AddOrUpdateAsync(discordGuild, (d => d.DiscordId == message.GuildId));
                discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(d => d.DiscordId == message.GuildId);

                #region Handle Channels
                foreach (SocketGuildChannel channel in guild.TextChannels)
                {
                    DiscordChannelUpdate channelUpdateContext = new DiscordChannelUpdate { GuildId = guild.Id, ChannelId = channel.Id };
                    await _bus.Publish(channelUpdateContext);
                }

                List<ulong> channelIDs = guild.TextChannels.Select(i => i.Id).Distinct().ToList();
                IEnumerable<DiscordChannel> dbChannels = await _work.ChannelRepository.FindAsync((d => d.DiscordGuild == discordGuild));
                foreach (DiscordChannel dbChannel in dbChannels)
                {
                    if (!channelIDs.Contains(dbChannel.DiscordId))
                    {
                        DiscordChannelDelete channelDeleteContext = new DiscordChannelDelete { GuildId = guild.Id, ChannelId = dbChannel.DiscordId };
                        await _bus.Publish(channelDeleteContext);
                    }
                }
                #endregion Handle Channels

                #region Handle Roles
                foreach (SocketRole role in guild.Roles)
                {
                    DiscordRoleUpdate roleUpdateContext = new DiscordRoleUpdate { GuildId = guild.Id, RoleId = role.Id };
                    await _bus.Publish(roleUpdateContext);
                }

                List<ulong> roleIDs = guild.Roles.Select(i => i.Id).Distinct().ToList();
                IEnumerable<DiscordRole> dbRoles = await _work.RoleRepository.FindAsync((d => d.DiscordGuild == discordGuild));
                foreach (DiscordRole dbRole in dbRoles)
                {
                    if (!roleIDs.Contains(dbRole.DiscordId))
                    {
                        DiscordRoleDelete roleDeleteContext = new DiscordRoleDelete { GuildId = guild.Id, RoleId = dbRole.DiscordId };
                        await _bus.Publish(roleDeleteContext);
                    }
                }
                #endregion Handle Roles
            }
            catch (Exception e)
            {
                Serilog.Log.Error($"{e}");
            }
        }
    }
}