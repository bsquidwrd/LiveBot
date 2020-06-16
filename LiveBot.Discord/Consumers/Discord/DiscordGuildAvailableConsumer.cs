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

                if (guild == null)
                    return;

                DiscordGuild discordGuild = new DiscordGuild() { DiscordId = message.GuildId, Name = message.GuildName };
                await _work.GuildRepository.AddOrUpdateAsync(discordGuild, (d => d.DiscordId == message.GuildId));
                discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(d => d.DiscordId == message.GuildId);

                #region Handle Channels
                var dbChannels = await _work.ChannelRepository.FindAsync(i => i.DiscordGuild == discordGuild);

                foreach (SocketGuildChannel channel in guild.TextChannels)
                {
                    var existingChannels = dbChannels.ToList().Where(i => i.DiscordId == channel.Id && i.Name == channel.Name );
                    if (existingChannels.Count() > 0)
                        continue;
                    DiscordChannelUpdate channelUpdateContext = new DiscordChannelUpdate { GuildId = guild.Id, ChannelId = channel.Id, ChannelName = channel.Name };
                    await _bus.Publish(channelUpdateContext);
                }

                List<ulong> channelIDs = guild.TextChannels.Select(i => i.Id).Distinct().ToList();
                //IEnumerable<DiscordChannel> dbChannels = await _work.ChannelRepository.FindAsync(i => i.DiscordGuild == discordGuild);
                if (dbChannels.Count() > 0)
                {
                    foreach (DiscordChannel dbChannel in dbChannels.Where(i => !channelIDs.Contains(i.DiscordId)))
                    {
                        DiscordChannelDelete channelDeleteContext = new DiscordChannelDelete { GuildId = guild.Id, ChannelId = dbChannel.DiscordId };
                        await _bus.Publish(channelDeleteContext);
                    }
                }

                #endregion Handle Channels

                #region Handle Roles
                var dbRoles = await _work.RoleRepository.FindAsync(i => i.DiscordGuild == discordGuild);

                foreach (SocketRole role in guild.Roles)
                {
                    var existingRoles = dbChannels.ToList().Where(i => i.DiscordId == role.Id && i.Name == role.Name);
                    if (existingRoles.Count() > 0)
                        continue;
                    DiscordRoleUpdate roleUpdateContext = new DiscordRoleUpdate { GuildId = guild.Id, RoleId = role.Id, RoleName = role.Name };
                    await _bus.Publish(roleUpdateContext);
                }

                List<ulong> roleIDs = guild.Roles.Select(i => i.Id).Distinct().ToList();
                if (dbRoles.Count() > 0)
                {
                    foreach (DiscordRole dbRole in dbRoles.Where(i => !roleIDs.Contains(i.DiscordId)))
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