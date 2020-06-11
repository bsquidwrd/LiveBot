using Discord;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Models.Streams;
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

        /// <summary>
        /// Finds and deletes all objects from the given <paramref name="guild"/> in the Database
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task _PurgeGuild(SocketGuild guild)
        {
            var discordGuild = await _AddOrUpdateGuild(guild);
            var discordChannels = await _work.ChannelRepository.FindAsync(d => d.DiscordGuild == discordGuild);
            var discordRoles = await _work.RoleRepository.FindAsync(d => d.DiscordGuild == discordGuild);
            var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(d => d.DiscordChannel.DiscordGuild == discordGuild);

            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                try
                {
                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
                }
                catch
                {
                    Log.Error($"Unable to remove Stream Subscription {streamSubscription.User.SourceID} {streamSubscription.User.Username} in {discordGuild.DiscordId} {discordGuild.Name}");
                    continue;
                }
            }

            foreach (DiscordChannel discordChannel in discordChannels)
            {
                try
                {
                    await _work.ChannelRepository.RemoveAsync(discordChannel.Id);
                }
                catch
                {
                    Log.Error($"Unable to remove Channel {discordChannel.DiscordId} {discordChannel.Name}");
                    continue;
                }
            }

            foreach (DiscordRole discordRole in discordRoles)
            {
                try
                {
                    await _work.RoleRepository.RemoveAsync(discordRole.Id);
                }
                catch
                {
                    Log.Error($"Unable to remove Role {discordRole.DiscordId} {discordRole.Name}");
                    continue;
                }
            }

            try
            {
                await _work.GuildRepository.RemoveAsync(discordGuild.Id);
            }
            catch
            {
                Log.Error($"Unable to remove Guild {discordGuild.DiscordId} {discordGuild.Name}");
            }
        }

        /// <summary>
        /// Creates or Updates the database for a given <paramref name="guild"/>
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task<DiscordGuild> _AddOrUpdateGuild(SocketGuild guild)
        {
            DiscordGuild discordGuild = new DiscordGuild() { DiscordId = guild.Id, Name = guild.Name };
            await _work.GuildRepository.AddOrUpdateAsync(discordGuild, (d => d.DiscordId == guild.Id));
            discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == guild.Id));
            return discordGuild;
        }

        /// <summary>
        /// Creates or Updates the database for a given <paramref name="channel"/>
        /// </summary>
        /// <param name="discordGuild"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task<DiscordChannel> _AddOrUpdateChannel(DiscordGuild discordGuild, SocketGuildChannel channel)
        {
            DiscordChannel discordChannel = new DiscordChannel() { DiscordGuild = discordGuild, DiscordId = channel.Id, Name = channel.Name };
            await _work.ChannelRepository.AddOrUpdateAsync(discordChannel, (d => d.DiscordGuild == discordGuild && d.DiscordId == channel.Id));
            discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(d => d.DiscordGuild == discordGuild && d.DiscordId == channel.Id);
            return discordChannel;
        }

        /// <summary>
        /// Creates or Updates the database for a given <paramref name="role"/>
        /// </summary>
        /// <param name="discordGuild"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<DiscordRole> _AddOrUpdateRole(DiscordGuild discordGuild, SocketRole role)
        {
            DiscordRole discordRole = new DiscordRole() { DiscordGuild = discordGuild, DiscordId = role.Id, Name = role.Name };
            await _work.RoleRepository.AddOrUpdateAsync(discordRole, (d => d.DiscordGuild == discordGuild && d.DiscordId == role.Id));
            discordRole = await _work.RoleRepository.SingleOrDefaultAsync(d => d.DiscordGuild == discordGuild && d.DiscordId == role.Id);
            return discordRole;
        }

        /// <summary>
        /// Processes and updates all Discord Channels the bot has access to in the given <paramref name="guild"/>
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task _UpdateGuildChannels(SocketGuild guild)
        {
            DiscordGuild discordGuild = await _AddOrUpdateGuild(guild);

            foreach (SocketGuildChannel channel in guild.Channels)
            {
                await _AddOrUpdateChannel(discordGuild, channel);
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

        /// <summary>
        /// Processes and updates all Discord Roles the bot has access to in the given <paramref name="guild"/>
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task _UpdateRoles(SocketGuild guild)
        {
            DiscordGuild discordGuild = await _AddOrUpdateGuild(guild);

            foreach (SocketRole role in guild.Roles)
            {
                await _AddOrUpdateRole(discordGuild, role);
            }

            List<ulong> roleIDs = new List<ulong>();
            foreach (SocketRole role in guild.Roles)
            {
                roleIDs.Add(role.Id);
            }

            IEnumerable<DiscordRole> dbRoles = await _work.RoleRepository.FindAsync((d => d.DiscordGuild == discordGuild));
            foreach (DiscordRole dbRole in dbRoles)
            {
                if (!roleIDs.Contains(dbRole.DiscordId))
                {
                    await _work.RoleRepository.RemoveAsync(dbRole.Id);
                    Log.Debug($"Removed role {dbRole.DiscordId} from {dbRole.DiscordGuild.Name}");
                }
            }
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="guild"/> becomes Available
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task GuildAvailable(SocketGuild guild)
        {
            await _AddOrUpdateGuild(guild);
            await _UpdateGuildChannels(guild);
            await _UpdateRoles(guild);
        }

        /// <summary>
        /// Discord Event Handler for when a Guild is updated from <paramref name="beforeGuild"/> to <paramref name="afterGuild"/>
        /// </summary>
        /// <param name="beforeGuild"></param>
        /// <param name="afterGuild"></param>
        /// <returns></returns>
        public async Task GuildUpdated(SocketGuild beforeGuild, SocketGuild afterGuild)
        {
            await _AddOrUpdateGuild(afterGuild);
        }

        /// <summary>
        /// Discord Event Handler for when the bot leaves a <paramref name="guild"/>
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task GuildLeave(SocketGuild guild)
        {
            await _PurgeGuild(guild);
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="channel"/> is created
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task ChannelCreated(SocketChannel channel)
        {
            try
            {
                SocketGuildChannel socketGuildChannel = (SocketGuildChannel)channel;
                DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == socketGuildChannel.Guild.Id));

                await _AddOrUpdateChannel(discordGuild, socketGuildChannel);
            }
            catch
            {
                Log.Error($"Error caught trying to Create channel. Channel {channel.Id}");
                return;
            }
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="channel"/> is destroyed/deleted
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
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
                Log.Error($"Error caught trying to Destroy channel. Channel {channel.Id}");
                return;
            }
        }

        /// <summary>
        /// Discord Event Handler for when a Channel is updated from <paramref name="beforeChannel"/> to <paramref name="afterChannel"/>
        /// </summary>
        /// <param name="beforeChannel"></param>
        /// <param name="afterChannel"></param>
        /// <returns></returns>
        public async Task ChannelUpdated(SocketChannel beforeChannel, SocketChannel afterChannel)
        {
            try
            {
                SocketGuildChannel beforeGuildChannel = (SocketGuildChannel)beforeChannel;
                SocketGuildChannel afterGuildChannel = (SocketGuildChannel)afterChannel;
                DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == beforeGuildChannel.Guild.Id));

                await _AddOrUpdateChannel(discordGuild, afterGuildChannel);
            }
            catch
            {
                Log.Error($"Error caught trying to Update channel. Channel {beforeChannel.Id}");
                return;
            }
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="role"/> is created
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task RoleCreated(SocketRole role)
        {
            try
            {
                DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == role.Guild.Id));
                await _AddOrUpdateRole(discordGuild, role);
            }
            catch
            {
                Log.Error($"Error caught trying to Create role. Guild {role.Guild.Id} {role.Guild.Name}, Role {role.Id} {role.Name}");
            }
            return;
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="role"/> is deleted
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task RoleDeleted(SocketRole role)
        {
            try
            {
                DiscordRole discordRole = await _work.RoleRepository.SingleOrDefaultAsync((d => d.DiscordId == role.Id));
                await _work.RoleRepository.RemoveAsync(discordRole.Id);
            }
            catch
            {
                Log.Error($"Error caught trying to Delete role. Guild {role.Guild.Id} {role.Guild.Name}, Role {role.Id} {role.Name}");
            }
            return;
        }

        /// <summary>
        /// Discord Event Handler for when a Role is updated from <paramref name="beforeRole"/> to <paramref name="afterRole"/>
        /// </summary>
        /// <param name="beforeRole"></param>
        /// <param name="afterRole"></param>
        /// <returns></returns>
        public async Task RoleUpdated(SocketRole beforeRole, SocketRole afterRole)
        {
            try
            {
                DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == beforeRole.Guild.Id));
                await _AddOrUpdateRole(discordGuild, afterRole);
            }
            catch
            {
                Log.Error($"Error caught trying to Update role. Guild {beforeRole.Guild.Id} {beforeRole.Guild.Name}, Role {afterRole.Id} {afterRole.Name}");
            }
            return;
        }

        /// <summary>
        /// Discord Event Handler for when a Guild Member is updated from <paramref name="beforeGuildUser"/> to <paramref name="afterGuildUser"/>.
        /// This should only be processed for when a Member changes to "Streaming".
        /// Is used to notify a <c>Guild</c> if someone has a role we have been told to notify for.
        /// </summary>
        /// <param name="beforeGuildUser"></param>
        /// <param name="afterGuildUser"></param>
        /// <returns></returns>
        public async Task GuildMemberUpdated(SocketGuildUser beforeGuildUser, SocketGuildUser afterGuildUser)
        {
            if (afterGuildUser.Guild.Id != 225471771355250688)
                return;
            if (beforeGuildUser.IsBot || afterGuildUser.IsBot)
            {
                // I don't care about bots
                return;
            }
            // This will be where we trigger events such as
            // when a user goes live in Discord to check if
            // the Guild has options enabled that allow notifying
            // when people with a certain role go live and verifying
            // that it's a legit stream etc etc
            await Task.Delay(1);
            IActivity userActivity = afterGuildUser.Activity;
            if (userActivity == null)
            {
                return;
            }
            if (userActivity.Type == ActivityType.Streaming && userActivity is StreamingGame)
            {
                StreamingGame userGame = (StreamingGame)userActivity;
                //Log.Information($"User changed to Streaming {afterGuildUser.Username}#{afterGuildUser.DiscriminatorValue} {userGame.Name} {userGame.Url}");

                foreach (SocketRole role in afterGuildUser.Roles)
                {
                    if (role.IsEveryone)
                    {
                        continue;
                    }
                    //Log.Information($"{role.Id} {role.Name}");
                }
            }
            return;
        }
    }
}