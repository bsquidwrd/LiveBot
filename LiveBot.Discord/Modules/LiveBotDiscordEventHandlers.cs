using Discord;
using Discord.WebSocket;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Discord.Contracts;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    internal class LiveBotDiscordEventHandlers
    {
        private readonly IBusControl _bus;
        private readonly IEnumerable<ILiveBotMonitor> _monitors;
        private readonly IUnitOfWork _work;

        public LiveBotDiscordEventHandlers(IServiceProvider services)
        {
            _bus = services.GetRequiredService<IBusControl>();
            _monitors = services.GetServices<ILiveBotMonitor>();

            IUnitOfWorkFactory factory = services.GetRequiredService<IUnitOfWorkFactory>();
            _work = factory.Create();
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="guild"/> becomes Available
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task GuildAvailable(SocketGuild guild)
        {
            var context = new DiscordGuildAvailable { GuildId = guild.Id, GuildName = guild.Name, IconUrl = guild.IconUrl };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a Guild is updated from <paramref name="beforeGuild"/> to
        /// <paramref name="afterGuild"/>
        /// </summary>
        /// <param name="beforeGuild"></param>
        /// <param name="afterGuild"></param>
        /// <returns></returns>
        public async Task GuildUpdated(SocketGuild beforeGuild, SocketGuild afterGuild)
        {
            if (beforeGuild.Name == afterGuild.Name)
                return;
            var context = new DiscordGuildUpdate { GuildId = afterGuild.Id, GuildName = afterGuild.Name, IconUrl = afterGuild.IconUrl };
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
            if (!(channel is SocketGuildChannel))
                return;
            SocketGuildChannel socketGuildChannel = (SocketGuildChannel)channel;
            var context = new DiscordChannelUpdate { GuildId = socketGuildChannel.Guild.Id, ChannelId = socketGuildChannel.Id, ChannelName = socketGuildChannel.Name };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="channel"/> is destroyed/deleted
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public async Task ChannelDestroyed(SocketChannel channel)
        {
            if (!(channel is SocketGuildChannel))
                return;
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
            if (!(beforeChannel is SocketTextChannel) || !(afterChannel is SocketTextChannel))
                return;
            SocketTextChannel beforeGuildChannel = (SocketTextChannel)beforeChannel;
            SocketTextChannel afterGuildChannel = (SocketTextChannel)afterChannel;

            if (beforeGuildChannel.Name == afterGuildChannel.Name)
                return;

            var context = new DiscordChannelUpdate { GuildId = afterGuildChannel.Guild.Id, ChannelId = afterGuildChannel.Id, ChannelName = afterGuildChannel.Name };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a <paramref name="role"/> is created
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task RoleCreated(SocketRole role)
        {
            var context = new DiscordRoleUpdate { GuildId = role.Guild.Id, RoleId = role.Id, RoleName = role.Name };
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
        /// Discord Event Handler for when a Role is updated from <paramref name="beforeRole"/> to
        /// <paramref name="afterRole"/>
        /// </summary>
        /// <param name="beforeRole"></param>
        /// <param name="afterRole"></param>
        /// <returns></returns>
        public async Task RoleUpdated(SocketRole beforeRole, SocketRole afterRole)
        {
            if (beforeRole.Name == afterRole.Name)
                return;
            var context = new DiscordRoleUpdate { GuildId = beforeRole.Guild.Id, RoleId = afterRole.Id, RoleName = afterRole.Name };
            await _bus.Publish(context);
        }

        /// <summary>
        /// Discord Event Handler for when a Guild Member is updated from <paramref name="beforeGuildUser"/> to <paramref name="afterGuildUser"/>.
        /// This should only be /// processed for when a Member changes to "Streaming".
        /// Is used to notify a <c>Guild</c> if someone has a role we have been told to notify for.
        /// </summary>
        /// <param name="beforeGuildUser"></param>
        /// <param name="afterGuildUser"></param>
        /// <returns></returns>
        public async Task GuildMemberUpdated(SocketGuildUser beforeGuildUser, SocketGuildUser afterGuildUser)
        {
            // I don't care about bots
            if (beforeGuildUser.IsBot || afterGuildUser.IsBot)
                return;

            // Check if the updated user has an activity set Also make sure it's a Streaming type of Activity
            IActivity userActivity = afterGuildUser.Activity;
            if (userActivity == null && userActivity?.Type != ActivityType.Streaming)
            {
                return;
            }

            // Check if the users activity is a Game
            if (userActivity is StreamingGame userGame)
            {
                // Make sure the Stream is supported by the bot
                var monitor = _monitors.Where(i => i.IsValid(userGame.Url)).FirstOrDefault();
                if (monitor == null) return;

                // Publish a Member Live Event for more in-depth checking
                var memberLivePayload = new DiscordMemberLive
                {
                    ServiceType = monitor.ServiceType,
                    Url = userGame.Url,
                    DiscordGuildId = afterGuildUser.Guild.Id,
                    DiscordUserId = afterGuildUser.Id
                };
                await _bus.Publish(memberLivePayload);
            }
        }
    }
}