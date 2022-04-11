using Discord;
using Discord.Interactions;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using LiveBot.Discord.SlashCommands.Attributes;
using LiveBot.Discord.SlashCommands.Helpers;
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Modules
{
    [RequireBotManager]
    [Group(name: "monitor", description: "Commands for manipulating stream monitors")]
    public class MonitorModule : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly ILogger<MonitorModule> _logger;
        private readonly IUnitOfWork _work;
        private readonly IEnumerable<ILiveBotMonitor> _monitors;

        public MonitorModule(ILogger<MonitorModule> logger, IUnitOfWorkFactory factory, IEnumerable<ILiveBotMonitor> monitors)
        {
            _logger = logger;
            _work = factory.Create();
            _monitors = monitors;
        }

        #region Slash Commands

        #region Create command

        /// <summary>
        /// Create a stream to monitor
        /// </summary>
        /// <param name="ProfileURL"></param>
        /// <param name="WhereToPost"></param>
        /// <param name="LiveMessage"></param>
        /// <param name="RoleToMention"></param>
        /// <returns></returns>
        [SlashCommand(name: "create", description: "Create a stream monitor")]
        public async Task StartStreamMonitor(
            [Summary(name: "profile-url", description: "The profile page of the streamer")]
            Uri ProfileURL,

            [Summary(name: "where-to-post", description: "The channel to post live alerts to")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)]
            IGuildChannel GuildChannel,

            [Summary(name: "live-message", description: "This message will be sent out when the streamer goes live (check /monitor help for more info)")]
            string LiveMessage = Defaults.NotificationMessage
        )
        {
            if (GuildChannel is not ITextChannel)
                throw new ArgumentException("Channel must be a text channel");
            var WhereToPost = (ITextChannel)GuildChannel;

            var monitor = GetMonitor(ProfileURL);

            LiveMessage = LiveMessage.Trim();
            if (String.IsNullOrWhiteSpace(LiveMessage)) LiveMessage = Defaults.NotificationMessage;
            if (String.Equals(LiveMessage, "default", StringComparison.InvariantCultureIgnoreCase))
                LiveMessage = Defaults.NotificationMessage;

            var allowedMentions = new AllowedMentions()
            {
                AllowedTypes = AllowedMentionTypes.None
            };

            var subscription = await EditStreamSubscriptionAsync(monitor: monitor, uri: ProfileURL, message: LiveMessage, guild: Context.Guild, channel: WhereToPost);
            var ResponseMessage = $"Success! I will post in {WhereToPost.Mention} when {Format.Bold(subscription.User.DisplayName)} goes live on {monitor.ServiceType} with the message {Format.Code(subscription.Message)} and mentioning the selected roles (if any)\n";

            var monitorUser = await monitor.GetUser(userId: subscription.User.SourceID);
            monitor.AddChannel(monitorUser);

            var roleSelectMenu = MonitorUtils.GetRoleMentionSelectMenu(subscription: subscription, guild: Context.Guild);
            var components = new ComponentBuilder().WithSelectMenu(roleSelectMenu).Build();

            await FollowupAsync(text: $"{ResponseMessage}\nUse the selection menu to choose a role to mention", ephemeral: true, allowedMentions: allowedMentions, components: components);
        }

        #endregion Create command

        #region Edit command

        /// <summary>
        /// Edit a stream monitor
        /// </summary>
        /// <param name="ProfileURL"></param>
        /// <param name="WhereToPost"></param>
        /// <param name="LiveMessage"></param>
        /// <param name="RoleToMention"></param>
        /// <returns></returns>
        [SlashCommand(name: "edit", description: "Edit a stream monitor")]
        public async Task EditStreamMonitor(
            [Summary(name: "profile-url", description: "The profile page of the streamer")]
            Uri ProfileURL,

            [Summary(name: "where-to-post", description: "The channel to post live alerts to")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)]
            IGuildChannel? GuildChannel = null,

            [Summary(name: "live-message", description: "This message will be sent out when the streamer goes live (check /monitor help for more info)")]
            string? LiveMessage = null
        )
        {
            ITextChannel? WhereToPost = null;
            if (GuildChannel is not ITextChannel && GuildChannel != null)
                throw new ArgumentException("Channel must be a text channel");
            if (GuildChannel != null)
                WhereToPost = (ITextChannel)GuildChannel;

            var monitor = GetMonitor(ProfileURL);

            var ResponseMessage = "";
            if (WhereToPost != null)
                ResponseMessage += $"Updated channel to {WhereToPost.Mention}. ";
            if (!String.IsNullOrWhiteSpace(LiveMessage))
            {
                if (LiveMessage.Equals("default", StringComparison.InvariantCultureIgnoreCase))
                    LiveMessage = Defaults.NotificationMessage;
                ResponseMessage += $"Updated message to {Format.Code(LiveMessage)}. ";
            }

            var subscription = await EditStreamSubscriptionAsync(monitor: monitor, uri: ProfileURL, message: LiveMessage, guild: Context.Guild, channel: WhereToPost);

            var allowedMentions = new AllowedMentions()
            {
                AllowedTypes = AllowedMentionTypes.None
            };

            if (WhereToPost != null || LiveMessage != null)
                ResponseMessage = $"Successfuly updated monitor for {Format.Bold(subscription.User.DisplayName)}! {ResponseMessage}";
            if (string.IsNullOrWhiteSpace(ResponseMessage))
                ResponseMessage = $"Nothing was updated for {Format.Bold(subscription.User.DisplayName)}";

            var roleSelectMenu = MonitorUtils.GetRoleMentionSelectMenu(subscription: subscription, guild: Context.Guild);
            var components = new ComponentBuilder().WithSelectMenu(roleSelectMenu).Build();

            await FollowupAsync(text: $"{ResponseMessage}\nUse the selection menu to choose a role to mention", ephemeral: true, allowedMentions: allowedMentions, components: components);
        }

        #endregion Edit command

        #region Delete command

        /// <summary>
        /// Delete a stream monitor with the given <see cref="Uri"/>
        /// </summary>
        /// <param name="ProfileURL"></param>
        /// <returns></returns>
        [SlashCommand(name: "delete", description: "Delete a stream from being monitored")]
        public async Task DeleteStreamMonitor(
            [Summary(name: "profile-url", description: "The profile page of the streamer")] Uri ProfileURL
        )
        {
            var monitor = GetMonitor(ProfileURL);

            var streamUser = await GetStreamUserAsync(monitor: monitor, uri: ProfileURL);

            var subscription = await _work.SubscriptionRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id && i.User == streamUser);
            if (subscription != null)
            {
                var displayName = Format.Bold(subscription.User.DisplayName);
                var resultMessage = "Unkown error occurred";
                try
                {
                    foreach (var roleToMention in subscription.RolesToMention)
                        await _work.RoleToMentionRepository.RemoveAsync(roleToMention.Id);
                    await _work.SubscriptionRepository.RemoveAsync(subscription.Id);

                    _logger.LogInformation(
                        message: "Stream Subscription deleted for {ServiceType} {StreamUsername} ({StreamUserId}) by {Username} ({UserId}) in {GuildName} ({GuildId}))",
                        subscription.User.ServiceType,
                        subscription.User.Username,
                        subscription.User.SourceID,
                        Format.UsernameAndDiscriminator(user: Context.User, doBidirectional: true),
                        Context.User.Id,
                        Context.Guild.Name,
                        Context.Guild.Id
                    );

                    resultMessage = $"Monitor for {displayName} has been deleted";
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        exception: ex,
                        message: "Stream Subscription could not be deleted for {ServiceType} {StreamUsername} ({StreamUserId}) by {Username} ({UserId}) in {GuildName} ({GuildId}))",
                        subscription.User.ServiceType,
                        subscription.User.Username,
                        subscription.User.SourceID,
                        Format.UsernameAndDiscriminator(user: Context.User, doBidirectional: true),
                        Context.User.Id,
                        Context.Guild.Name,
                        Context.Guild.Id
                    );

                    resultMessage = $"Unable to delete monitor for {displayName}";
                }

                await FollowupAsync(text: resultMessage, ephemeral: true);
            }
            else
            {
                await FollowupAsync($"No subscription found for {Format.Bold(streamUser.DisplayName)}");
            }
        }

        #endregion Delete command

        #region Help command

        /// <summary>
        /// Return help information to better explain placeholders
        /// for the Live Message that can be sent
        /// </summary>
        /// <returns></returns>
        [SlashCommand(name: "help", description: "Get some help with setting up a stream monitor")]
        public async Task HelpStreamMonitor()
        {
            var message = @$"
With the {Format.Code("live-message")} portion of the commands, you can use the placeholders below and must include the {Format.Code("{")} and {Format.Code("}")}.
Using these placeholders {Format.Bold("AS IS")} will {Format.Bold("AUTOMATICALLY")} replace them with their respective values when the bot sends a message

Example (default):
{Format.Code(Defaults.NotificationMessage)}

{Format.Bold("will AUTOMATICALLY turn into")}

{Format.Code("@Live bsquidwrd is live and playing Valorant! https://twitch.tv/bsquidwrd")}

{Format.Code("{role}")} = The role that you setup to be pinged (if applicable)
{Format.Code("{name}")} = The display name of the Streamer
{Format.Code("{game}")} = Game being played at the time of going live
{Format.Code("{url}")} = Link to the stream
{Format.Code("{title}")} = Stream title at the time of going live

You can find a full guide here: {Format.EscapeUrl("https://bsquidwrd.gitbook.io/livebot-docs/tutorial-walkthrough/start-monitoring-a-stream")}
";
            await FollowupAsync(message, ephemeral: true);
        }

        #endregion Help command

        #region Check command

        /// <summary>
        /// Run a force check to see if a stream is live or not
        /// </summary>
        /// <param name="ProfileURL"></param>
        /// <returns></returns>
        [SlashCommand(name: "check", description: "Check that the bot can tell someone is live")]
        public async Task CheckStreamAsync(
            [Summary(name: "profile-url", description: "The profile page of the streamer")] Uri ProfileURL
        )
        {
            var monitor = GetMonitor(uri: ProfileURL);
            var user = await monitor.GetUser(profileURL: ProfileURL.AbsoluteUri);
            var stream = await monitor.GetStream_Force(user: user);
            if (stream == null)
            {
                await FollowupAsync(text: $"Looks like {user.DisplayName} is not live right now", ephemeral: true);
            }
            else
            {
                var streamEmbed = NotificationHelpers.GetStreamEmbed(stream: stream, user: stream.User, game: stream.Game);
                var bogusSubscription = new StreamSubscription() { Message = Defaults.NotificationMessage };
                var notificationMessage = NotificationHelpers.GetNotificationMessage(stream: stream, subscription: bogusSubscription);
                await FollowupAsync(text: notificationMessage, embed: streamEmbed, ephemeral: true);
            }
        }

        #endregion Check command

        #region Role command

        /// <summary>
        /// Setup to monitor a role in the server
        /// </summary>
        /// <param name="WhereToPost"></param>
        /// <param name="LiveMessage"></param>
        /// <param name="RoleToMention"></param>
        /// <param name="RoleToMonitor"></param>
        /// <param name="StopMonitoring"></param>
        /// <returns></returns>
        [SlashCommand(name: "role", description: "Used to start monitoring a role instead of a specific user")]
        public async Task MonitorRoleAsync(
            [Summary(name: "where-to-post", description: "The channel to post live alerts to when this role goes live")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)]
            IGuildChannel? GuildChannel = null,

            [Summary(name: "live-message", description: "This message will be sent out when the streamer goes live (check /monitor help)")]
            string LiveMessage = Defaults.NotificationMessage,

            [Summary(name: "role-to-mention", description: "The role to replace {role} with in the live message (default is none)")]
            IRole? RoleToMention = null,

            [Summary(name: "role-to-monitor", description: "The role to monitor for when they go live")]
            IRole? RoleToMonitor = null,

            [Summary(name: "stop-monitoring", description: "Stop monitoring a role")]
            bool StopMonitoring = false
        )
        {
            ITextChannel? WhereToPost = null;
            if (GuildChannel is not ITextChannel && GuildChannel != null)
                throw new ArgumentException("Channel must be a text channel");
            if (GuildChannel != null)
                WhereToPost = (ITextChannel)GuildChannel;

            if (WhereToPost == null && LiveMessage == null && RoleToMention == null && RoleToMonitor == null && !StopMonitoring)
            {
                await FollowupAsync(text: $"Nothing was updated", ephemeral: true);
                return;
            }

            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == Context.Guild.Id);
            if (discordGuild == null)
            {
                var newDiscordGuild = new DiscordGuild
                {
                    DiscordId = Context.Guild.Id,
                    Name = Context.Guild.Name,
                    IconUrl = Context.Guild.IconId
                };
                await _work.GuildRepository.AddOrUpdateAsync(newDiscordGuild, i => i.DiscordId == Context.Guild.Id);
            }
            discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == Context.Guild.Id);

            var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id);
            if (guildConfig == null)
            {
                var newGuildConfig = new DiscordGuildConfig
                {
                    DiscordGuild = discordGuild
                };
                await _work.GuildConfigRepository.AddOrUpdateAsync(newGuildConfig, i => i.DiscordGuild.DiscordId == Context.Guild.Id);
            }
            guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id);

            if (WhereToPost != null)
                guildConfig.DiscordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id && i.DiscordId == WhereToPost.Id);

            if (LiveMessage != null)
            {
                if (LiveMessage.Equals("default", StringComparison.InvariantCultureIgnoreCase))
                {
                    LiveMessage = Defaults.NotificationMessage;
                }
                guildConfig.Message = LiveMessage;
            }

            if (RoleToMonitor != null)
                guildConfig.MonitorRoleDiscordId = RoleToMonitor.Id;

            if (RoleToMention != null)
                guildConfig.MentionRoleDiscordId = RoleToMention.Id;

            if (StopMonitoring)
            {
                guildConfig.MonitorRoleDiscordId = null;
                guildConfig.MentionRoleDiscordId = null;
                guildConfig.DiscordChannel = null;
                guildConfig.Message = null;
            }

            await _work.GuildConfigRepository.UpdateAsync(guildConfig);

            await FollowupAsync(text: $"Updated role monitoring config", ephemeral: true);
        }

        #endregion Role command

        #region List command

        /// <summary>
        /// List all stream monitors in the Guild
        /// </summary>
        /// <returns></returns>
        [SlashCommand(name: "list", description: "List all stream monitors")]
        public async Task ListStreamMonitorAsync()
        {
            var subscriptions = await _work.SubscriptionRepository.FindAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id);
            subscriptions = subscriptions.OrderBy(i => i.User.DisplayName);

            if (!subscriptions.Any())
            {
                await FollowupAsync("There are no subscriptions for this server!", ephemeral: true);
                return;
            }

            var subscription = subscriptions.First();

            var subscriptionEmbed = MonitorUtils.GetSubscriptionEmbed(currentSpot: 0, subscription: subscription, subscriptionCount: subscriptions.Count());
            var messageComponents = MonitorUtils.GetSubscriptionComponents(subscription: subscription, previousSpot: -1, nextSpot: 1);

            await FollowupAsync(text: $"Streams being monitored for this server", ephemeral: true, embed: subscriptionEmbed, components: messageComponents);
        }

        #endregion List command

        #endregion Slash Commands

        #region Misc Helpers

        /// <summary>
        /// Get the appropriate <see cref="ILiveBotMonitor"/> for the given <see cref="Uri"/>
        /// </summary>
        /// <param name="uri"></param>
        /// <returns><see cref="ILiveBotMonitor"/></returns>
        /// <exception cref="ArgumentException"></exception>
        private ILiveBotMonitor GetMonitor(Uri uri)
        {
            var monitor = _monitors.Where(x => x.IsValid(uri.AbsoluteUri)).FirstOrDefault();
            if (monitor == null)
                throw new ArgumentException($"Invalid/unsupported Profile URL {Format.EscapeUrl(uri.AbsoluteUri)}");
            return monitor;
        }

        /// <summary>
        /// Returns <see cref="StreamUser"/> from the given <see cref="ILiveBotMonitor"/> and <see cref="Uri"/>
        /// </summary>
        /// <param name="monitor"></param>
        /// <param name="uri"></param>
        /// <returns><see cref="StreamUser"/></returns>
        private async Task<StreamUser> GetStreamUserAsync(ILiveBotMonitor monitor, Uri uri)
        {
            var monitorUser = await monitor.GetUser(profileURL: uri.AbsoluteUri);
            StreamUser streamUser = new()
            {
                ServiceType = monitorUser.ServiceType,
                SourceID = monitorUser.Id,
                Username = monitorUser.Username,
                DisplayName = monitorUser.DisplayName,
                AvatarURL = monitorUser.AvatarURL,
                ProfileURL = monitorUser.ProfileURL
            };

            Expression<Func<StreamUser, bool>> streamUserPredicate = (i =>
                i.ServiceType == monitorUser.ServiceType &&
                i.SourceID == monitorUser.Id
            );

            await _work.UserRepository.AddOrUpdateAsync(streamUser, streamUserPredicate);
            return await _work.UserRepository.SingleOrDefaultAsync(streamUserPredicate);
        }

        /// <summary>
        /// Edit portions of a <see cref="StreamSubscription"/>
        /// </summary>
        /// <param name="monitor"></param>
        /// <param name="uri"></param>
        /// <param name="guild"></param>
        /// <param name="channel"></param>
        /// <param name="role"></param>
        /// <param name="message"></param>
        /// <param name="RemoveRole"></param>
        /// <returns><see cref="StreamSubscription"/></returns>
        private async Task<StreamSubscription> EditStreamSubscriptionAsync(ILiveBotMonitor monitor, Uri uri, IGuild guild, ITextChannel? channel, string? message = null)
        {
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(x => x.DiscordId == guild.Id);
            DiscordChannel? discordChannel = null;
            if (channel != null)
                discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(x => x.DiscordId == channel.Id);

            var streamUser = await GetStreamUserAsync(monitor, uri);

            Expression<Func<StreamSubscription, bool>> streamSubscriptionPredicate = (i =>
                i.User == streamUser &&
                i.DiscordGuild == discordGuild
            );

            var subscription = await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);
            if (subscription == null)
            {
                var newSubscription = new StreamSubscription()
                {
                    User = streamUser,
                    DiscordGuild = discordGuild
                };
                await _work.SubscriptionRepository.AddAsync(newSubscription);
                subscription = await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);
            }

            if (discordChannel != null)
                subscription.DiscordChannel = discordChannel;
            if (message != null)
                subscription.Message = message;

            if (subscription.RolesToMention != null)
                if (subscription.RolesToMention.Any() && !subscription.Message.Contains("{role}", StringComparison.InvariantCultureIgnoreCase))
                    subscription.Message = "{role} " + subscription.Message;

            await _work.SubscriptionRepository.UpdateAsync(subscription);

            return await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);
        }

        #endregion Misc Helpers
    }
}