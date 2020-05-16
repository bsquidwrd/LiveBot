using Discord.WebSocket;
using LiveBot.Core.Repository;
using LiveBot.Core.Repository.Models.Discord;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    internal class LoadGuildInformation
    {
        private readonly IUnitOfWork _work;

        public LoadGuildInformation(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        public async Task _PurgeGuild(SocketGuild guild)
        {
            DiscordGuild discordGuild = new DiscordGuild() { DiscordId = guild.Id, Name = guild.Name };
            await _work.GuildRepository.AddOrUpdateAsync(discordGuild, (d => d.DiscordId == guild.Id));
            discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == guild.Id));

            foreach (SocketGuildChannel channel in guild.Channels)
            {
                DiscordChannel discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync((d => d.DiscordId == channel.Id));
                await _work.ChannelRepository.RemoveAsync(discordChannel.Id);
            }

            try
            {
                DiscordChannel guildDefaultChannel = await _work.ChannelRepository.SingleOrDefaultAsync((d => d.DiscordId == guild.Id));
                await _work.ChannelRepository.RemoveAsync(guildDefaultChannel.Id);
            }
            catch
            {
            }

            await _work.GuildRepository.RemoveAsync(discordGuild.Id);
        }

        public async Task _UpdateGuildChannels(SocketGuild guild)
        {
            DiscordGuild discordGuild = new DiscordGuild() { DiscordId = guild.Id, Name = guild.Name };
            await _work.GuildRepository.AddOrUpdateAsync(discordGuild, (d => d.DiscordId == guild.Id));
            discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == guild.Id));

            foreach (SocketGuildChannel channel in guild.Channels)
            {
                DiscordChannel discordChannel = new DiscordChannel() { DiscordGuild = discordGuild, DiscordId = channel.Id, Name = channel.Name };
                await _work.ChannelRepository.AddOrUpdateAsync(discordChannel, (c => c.DiscordGuild == discordGuild && c.DiscordId == channel.Id));
            }

            List<ulong> channelIDs = new List<ulong>();
            foreach (SocketGuildChannel channel in guild.Channels)
            {
                channelIDs.Add(channel.Id);
            }

            IEnumerable<DiscordChannel> dbChannels = await _work.ChannelRepository.FindAsync((d => d.DiscordGuild == discordGuild));
            foreach (DiscordChannel dbChannel in dbChannels)
            {
                if (!channelIDs.Contains(dbChannel.DiscordId))
                {
                    await _work.ChannelRepository.RemoveAsync(dbChannel.Id);
                }
            }
        }

        public async Task GuildAvailable(SocketGuild guild)
        {
            Log.Information($@"Guild Available {guild.Name}");
            await _UpdateGuildChannels(guild);
        }

        public async Task GuildLeave(SocketGuild guild)
        {
            Log.Information($@"Left Guild {guild.Name}");
            await _PurgeGuild(guild);
        }

        public async Task ChannelCreated(SocketChannel channel)
        {
            try
            {
                SocketGuildChannel socketGuildChannel = (SocketGuildChannel)channel;
                DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == socketGuildChannel.Guild.Id));
                DiscordChannel discordChannel = new DiscordChannel() { DiscordGuild = discordGuild, DiscordId = socketGuildChannel.Id, Name = socketGuildChannel.Name };
                await _work.ChannelRepository.AddOrUpdateAsync(discordChannel, (c => c.DiscordGuild == discordGuild && c.DiscordId == socketGuildChannel.Id));
            }
            catch
            {
                Log.Error("Error caught trying to Create channel");
                return;
            }
        }

        public async Task ChannelDestroyed(SocketChannel channel)
        {
            try
            {
                SocketGuildChannel socketGuildChannel = (SocketGuildChannel)channel;
                DiscordChannel discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync((d => d.DiscordId == socketGuildChannel.Id));
                await _work.ChannelRepository.RemoveAsync(discordChannel.Id);
            }
            catch
            {
                Log.Error("Error caught trying to Destroy channel");
                return;
            }
        }

        public async Task ChannelUpdated(SocketChannel beforeChannel, SocketChannel afterChannel)
        {
            try
            {
                SocketGuildChannel beforeGuildChannel = (SocketGuildChannel)beforeChannel;
                SocketGuildChannel afterGuildChannel = (SocketGuildChannel)afterChannel;
                DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == beforeGuildChannel.Guild.Id));
                DiscordChannel discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync((d => d.DiscordId == beforeChannel.Id));
                discordChannel.Name = afterGuildChannel.Name;

                await _work.ChannelRepository.UpdateAsync(discordChannel);
            }
            catch
            {
                Log.Error("Error caught trying to Update channel");
                return;
            }
        }

        public async Task RoleCreated(SocketRole socketRole)
        {
            Log.Information($@"Role Created {socketRole.Name}");
            return;
        }

        public async Task RoleDeleted(SocketRole socketRole)
        {
            Log.Information($@"Role Deleted {socketRole.Name}");
            return;
        }

        public async Task RoleUpdated(SocketRole beforeRole, SocketRole afterRole)
        {
            Log.Information($@"Role Updated {beforeRole.Name} to {afterRole.Name}");
            return;
        }
    }
}