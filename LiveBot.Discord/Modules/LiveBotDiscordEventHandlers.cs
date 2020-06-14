using Discord;
using Discord.WebSocket;
using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.Contracts;
using MassTransit;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    internal class LiveBotDiscordEventHandlers
    {
        private readonly IBusControl _bus;

        public LiveBotDiscordEventHandlers(IBusControl bus)
        {
            _bus = bus;
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="guild"/> becomes Available
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task GuildAvailable(SocketGuild guild)
        {
            var context = new DiscordGuildUpdate { GuildId = guild.Id };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a Guild is updated from <paramref name="beforeGuild"/> to <paramref name="afterGuild"/>
        /// </summary>
        /// <param name="beforeGuild"></param>
        /// <param name="afterGuild"></param>
        /// <returns></returns>
        public async Task GuildUpdated(SocketGuild beforeGuild, SocketGuild afterGuild)
        {
            var context = new DiscordGuildUpdate { GuildId = beforeGuild.Id };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when the bot leaves a <paramref name="guild"/>
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task GuildLeave(SocketGuild guild)
        {
            var context = new DiscordGuildDelete { GuildId = guild.Id };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="channel"/> is created
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task ChannelCreated(SocketChannel channel)
        {
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)channel;
            var context = new DiscordChannelUpdate { GuildId = socketGuildChannel.Guild.Id, ChannelId = socketGuildChannel.Id };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="channel"/> is destroyed/deleted
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task ChannelDestroyed(SocketChannel channel)
        {
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)channel;
            var context = new DiscordChannelDelete { GuildId = socketGuildChannel.Guild.Id, ChannelId = socketGuildChannel.Id };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a Channel is updated from <paramref name="beforeChannel"/> to <paramref name="afterChannel"/>
        /// </summary>
        /// <param name="beforeChannel"></param>
        /// <param name="afterChannel"></param>
        /// <returns></returns>
        public async Task ChannelUpdated(SocketChannel beforeChannel, SocketChannel afterChannel)
        {
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)beforeChannel;
            var context = new DiscordChannelUpdate { GuildId = socketGuildChannel.Guild.Id, ChannelId = socketGuildChannel.Id };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="role"/> is created
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task RoleCreated(SocketRole role)
        {
            var context = new DiscordRoleUpdate { GuildId = role.Guild.Id, RoleId = role.Id };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="role"/> is deleted
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task RoleDeleted(SocketRole role)
        {
            var context = new DiscordRoleDelete { GuildId = role.Guild.Id, RoleId = role.Id };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a Role is updated from <paramref name="beforeRole"/> to <paramref name="afterRole"/>
        /// </summary>
        /// <param name="beforeRole"></param>
        /// <param name="afterRole"></param>
        /// <returns></returns>
        public async Task RoleUpdated(SocketRole beforeRole, SocketRole afterRole)
        {
            var context = new DiscordRoleUpdate { GuildId = beforeRole.Guild.Id, RoleId = beforeRole.Id };
            await _bus.Publish(context);
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