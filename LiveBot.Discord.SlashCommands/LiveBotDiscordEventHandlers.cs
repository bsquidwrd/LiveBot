using Discord;
using Discord.WebSocket;
using LiveBot.Discord.SlashCommands.Contracts.Discord;
using MassTransit;
using System.Collections.Concurrent;

namespace LiveBot.Discord.SlashCommands
{
    public class LiveBotDiscordEventHandlers
    {
        private readonly IBusControl _bus;
        private readonly ILogger<LiveBotDiscordEventHandlers> _logger;
        private readonly System.Timers.Timer emptyPresenceQueuedCacheTimer;
        private ConcurrentBag<ulong> presenceQueuedCache = new();

        public LiveBotDiscordEventHandlers(IBusControl bus, ILogger<LiveBotDiscordEventHandlers> logger)
        {
            _bus = bus;
            _logger = logger;

            // Setup the timer to clear the presenceQueuedCache
            emptyPresenceQueuedCacheTimer = new System.Timers.Timer()
            {
                AutoReset = true,
                Interval = TimeSpan.FromMinutes(5).TotalMilliseconds,
                Enabled = true,
            };
            emptyPresenceQueuedCacheTimer.Elapsed += EmptyPresenceQueuedCache;
            emptyPresenceQueuedCacheTimer.Start();
        }

        /// <summary>
        /// Updates the shards status and game
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public async Task OnReady(DiscordSocketClient client)
        {
            await client.SetStatusAsync(UserStatus.Online);
            await client.SetGameAsync(name: "Your Stream!", type: ActivityType.Watching);
            _logger.LogInformation("Shard {ShardId} ready", client.ShardId);
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
        /// Discord Event Handler for when a <paramref name="guild"/> is Joined
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task GuildJoined(SocketGuild guild)
        {
            _logger.LogInformation(message: "Joined Guild {GuildName} ({GuildId})", guild.Name, guild.Id.ToString());
            await Task.CompletedTask;
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
            if (
                beforeGuild.Name.Equals(afterGuild.Name, StringComparison.InvariantCultureIgnoreCase) &&
                beforeGuild.IconUrl.Equals(afterGuild.IconUrl, StringComparison.InvariantCultureIgnoreCase)
            )
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
            _logger.LogInformation(message: "Left Guild {GuildName} ({GuildId})", guild.Name, guild.Id.ToString());
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
            if (channel is not SocketGuildChannel socketGuildChannel)
                return;
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
            if (channel is not SocketGuildChannel socketGuildChannel)
                return;
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
            if (beforeChannel is not SocketTextChannel || afterChannel is not SocketTextChannel)
                return;
            SocketTextChannel beforeGuildChannel = (SocketTextChannel)beforeChannel;
            SocketTextChannel afterGuildChannel = (SocketTextChannel)afterChannel;

            if (beforeGuildChannel.Name.Equals(afterGuildChannel.Name, StringComparison.InvariantCultureIgnoreCase))
                return;

            var context = new DiscordChannelUpdate { GuildId = afterGuildChannel.Guild.Id, ChannelId = afterGuildChannel.Id, ChannelName = afterGuildChannel.Name };
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
        /// Discord Event Handler for when a Guild Member is updated from <paramref name="beforeGuildUser"/> to <paramref name="afterGuildUser"/>.
        /// This should only be /// processed for when a Member changes to "Streaming".
        /// Is used to notify a <c>Guild</c> if someone has a role we have been told to notify for.
        /// </summary>
        /// <param name="beforeGuildUser"></param>
        /// <param name="afterGuildUser"></param>
        /// <returns></returns>
        public async Task PresenceUpdated(SocketUser user, SocketPresence beforePresence, SocketPresence afterPresence)
        {
            // I don't care about bots
            if (user.IsBot)
                return;

            // Check if the user was previously Streaming, and if so skip it
            foreach (var userActivity in beforePresence.Activities)
            {
                if (userActivity.Type == ActivityType.Streaming && userActivity is StreamingGame game)
                {
                    // If there's no URL, then skip
                    if (String.IsNullOrWhiteSpace(game.Url))
                        continue;

                    return;
                }
            }

            // Check if the updated user has an activity set Also make sure it's a Streaming type of Activity
            StreamingGame? userGame = null;
            foreach (var userActivity in afterPresence.Activities)
            {
                if (userActivity.Type == ActivityType.Streaming && userActivity is StreamingGame game)
                {
                    // If there's no URL, then skip
                    if (String.IsNullOrWhiteSpace(game.Url))
                        continue;

                    userGame = game;
                    break;
                }
            }

            // Incase one couldn't be found, skip
            if (userGame == null)
                return;

            // If the user was previously queued, skip
            // otherwise add them to queued cache
            if (presenceQueuedCache.Contains(user.Id))
                return;
            else
                presenceQueuedCache.Add(user.Id);

            // Publish a Member Live Event for processing
            await _bus.Publish(new DiscordMemberLive
            {
                DiscordUserId = user.Id,
                Url = userGame.Url,
                GameName = userGame.Name,
                GameDetails = userGame.Details,
            });
        }

        /// <summary>
        /// Fired to empty the cache of users that were already queued for
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void EmptyPresenceQueuedCache(object? sender = null, System.Timers.ElapsedEventArgs? args = null) =>
            presenceQueuedCache = new();
    }
}