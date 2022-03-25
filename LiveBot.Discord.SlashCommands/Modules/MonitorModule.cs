using Discord;
using Discord.Interactions;
using Discord.Rest;
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
    public partial class MonitorModule : RestInteractionModuleBase<RestInteractionContext>
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
            [Summary(name: "profile-url", description: "The profile page of the streamer")] Uri ProfileURL,
            [Summary(name: "where-to-post", description: "The channel to post live alerts to")] ITextChannel WhereToPost,
            [Summary(name: "live-message", description: "This message will be sent out when the streamer goes live (check /monitor help for more info)")] string LiveMessage = Defaults.NotificationMessage,
            [Summary(name: "role-to-mention", description: "The role to replace {role} with in the live message (default is none)")] IRole? RoleToMention = null
        )
        {
            var monitor = GetMonitor(ProfileURL);

            LiveMessage = LiveMessage.Trim();
            if (String.IsNullOrWhiteSpace(LiveMessage)) LiveMessage = Defaults.NotificationMessage;
            if (String.Equals(LiveMessage, "default", StringComparison.InvariantCultureIgnoreCase))
                LiveMessage = Defaults.NotificationMessage;

            var allowedMentions = new AllowedMentions()
            {
                AllowedTypes = AllowedMentionTypes.None
            };

            var subscription = await EditStreamSubscriptionAsync(monitor: monitor, uri: ProfileURL, message: LiveMessage, guild: Context.Guild, channel: WhereToPost, role: RoleToMention);
            var ResponseMessage = $"Success! I will post in {WhereToPost.Mention} when {Format.Bold(subscription.User.DisplayName)} goes live on {monitor.ServiceType} with the message {Format.Code(subscription.Message)} and mentioning {RoleToMention?.Mention ?? "nobody"}\n";

            await FollowupAsync(text: ResponseMessage, ephemeral: true, allowedMentions: allowedMentions);
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
            [Summary(name: "profile-url", description: "The profile page of the streamer")] Uri ProfileURL,
            [Summary(name: "where-to-post", description: "The channel to post live alerts to")] ITextChannel? WhereToPost = null,
            [Summary(name: "live-message", description: "This message will be sent out when the streamer goes live (check /monitor help for more info)")] string? LiveMessage = null,
            [Summary(name: "role-to-mention", description: "The role to replace {role} with in the live message (default is none)")] IRole? RoleToMention = null,
            [Summary(name: "remove-role-ping", description: "True means the role to ping will be removed, False will leave the role to be pinged")] bool RemoveRolePing = false
        )
        {
            var monitor = GetMonitor(ProfileURL);

            var ResponseMessage = "";
            if (WhereToPost != null)
                ResponseMessage += $"Updated channel to {WhereToPost.Mention}. ";
            if (RoleToMention != null)
                ResponseMessage += $"Updated role to {RoleToMention.Mention}. ";
            if (!String.IsNullOrWhiteSpace(LiveMessage))
            {
                if (LiveMessage.Equals("default", StringComparison.InvariantCultureIgnoreCase))
                    LiveMessage = Defaults.NotificationMessage;
                ResponseMessage += $"Updated message to {Format.Code(LiveMessage)}. ";
            }

            var subscription = await EditStreamSubscriptionAsync(monitor: monitor, uri: ProfileURL, message: LiveMessage, guild: Context.Guild, channel: WhereToPost, role: RoleToMention, RemoveRole: RemoveRolePing);

            var allowedMentions = new AllowedMentions()
            {
                AllowedTypes = AllowedMentionTypes.None
            };

            if (WhereToPost != null || LiveMessage != null || RoleToMention != null || RemoveRolePing)
                ResponseMessage = $"Successfuly updated monitor for {Format.Bold(subscription.User.DisplayName)}! {ResponseMessage}";
            await FollowupAsync(text: ResponseMessage, ephemeral: true, allowedMentions: allowedMentions);
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
                await _work.SubscriptionRepository.RemoveAsync(subscription.Id);
                await FollowupAsync($"Successfully deleted the monitor for {Format.Bold(streamUser.DisplayName)}", ephemeral: true);
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
        private async Task<StreamSubscription> EditStreamSubscriptionAsync(ILiveBotMonitor monitor, Uri uri, IGuild guild, ITextChannel? channel, IRole? role = null, string? message = null, bool RemoveRole = false)
        {
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(x => x.DiscordId == guild.Id);
            DiscordChannel? discordChannel = null;
            DiscordRole? discordRole = null;
            if (channel != null)
                discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(x => x.DiscordId == channel.Id);
            if (role != null)
                discordRole = await _work.RoleRepository.SingleOrDefaultAsync(x => x.DiscordId == role.Id);

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
            if (discordRole != null)
                subscription.DiscordRole = discordRole;
            if (message != null)
                subscription.Message = message;
            if (RemoveRole)
                subscription.DiscordRole = null;

            if (subscription.DiscordRole != null && !subscription.Message.Contains("{role}", StringComparison.InvariantCultureIgnoreCase))
                subscription.Message = "{role} " + subscription.Message;

            await _work.SubscriptionRepository.UpdateAsync(subscription);

            return await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);
        }

        #endregion Misc Helpers
    }
}