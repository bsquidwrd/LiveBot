using Discord;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    internal class LiveBotDiscordEventHandlers
    {
        private readonly IUnitOfWork _work;

        public LiveBotDiscordEventHandlers(IUnitOfWorkFactory factory)
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
            //Log.Information($@"Guild Available {guild.Name}");
            await _UpdateGuildChannels(guild);

            try
            {
                SocketGuildUser socketGuildUser = guild.GetUser(131224383640436736);
                Log.Information($@"--------------------------------------------------");
                Log.Information($@"Guild {guild.Id} {guild.Name}");
                Log.Information($@"Roles for {socketGuildUser.Username}#{socketGuildUser.DiscriminatorValue}");
                foreach (SocketRole role in socketGuildUser.Roles)
                {
                    if (role == guild.EveryoneRole)
                    {
                        continue;
                    }
                    Log.Information($@"{role.Id} {role.Name}");
                }
                Log.Information($@"--------------------------------------------------");
            }
            catch
            {
            }
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
            await Task.Delay(1);
            Log.Information($@"Role Created {socketRole.Name}");
            return;
        }

        public async Task RoleDeleted(SocketRole socketRole)
        {
            await Task.Delay(1);
            Log.Information($@"Role Deleted {socketRole.Name}");
            return;
        }

        public async Task RoleUpdated(SocketRole beforeRole, SocketRole afterRole)
        {
            await Task.Delay(1);
            Log.Information($@"Role Updated {beforeRole.Name} to {afterRole.Name}");
            return;
        }

        public async Task GuildMemberUpdated(SocketGuildUser beforeGuildUser, SocketGuildUser afterGuildUser)
        {
            if (beforeGuildUser.IsBot || afterGuildUser.IsBot)
            {
                // I don't care about bots going live
                return;
            }
            // This will be where we trigger events such as
            // when a user goes live in Discord to check if
            // the Guild has options enabled that allow notifying
            // when people with a certain role go live and verifying
            // that it's a legit stream etc etc
            await Task.Delay(1);
            IActivity userActivity = afterGuildUser.Activity;
            if (userActivity.Type == ActivityType.Streaming && userActivity is StreamingGame)
            {
                StreamingGame userGame = (StreamingGame)userActivity;
                Log.Information($@"User changed to Streaming {afterGuildUser.Username}#{afterGuildUser.DiscriminatorValue} {userGame.Name} {userGame.Url}");

                foreach (SocketRole role in afterGuildUser.Roles)
                {
                    if (role.Id == afterGuildUser.Guild.Id)
                    {
                        continue;
                    }
                    Log.Information($@"{role.Id} {role.Name}");
                }
            }
            return;
        }
    }
}