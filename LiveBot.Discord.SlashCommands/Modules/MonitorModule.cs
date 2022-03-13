using Discord;
using Discord.Interactions;
using Discord.Rest;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Discord.SlashCommands.Attributes;
using LiveBot.Discord.SlashCommands.Helpers;
using System.Linq.Expressions;

namespace LiveBot.Discord.SlashCommands.Modules
{
    [RequireBotManager]
    [Group(name: "monitor", description: "Commands for manipulating stream monitors")]
    public class MonitorModule : RestInteractionModuleBase<RestInteractionContext>
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

        private ILiveBotMonitor GetMonitor(Uri uri)
        {
            var monitor = _monitors.Where(x => x.IsValid(uri.AbsoluteUri)).FirstOrDefault();
            if (monitor == null)
                throw new ArgumentException($"Invalid/unsupported Profile URL {Format.EscapeUrl(uri.AbsoluteUri)}");
            return monitor;
        }

        [SlashCommand(name: "autocreate", description: "Try to automatically setup a monitor")]
        public async Task AutoStartStreamMonitor(
            [Summary(name: "live-message", description: "An example of what you want the bot to send (don't mention a game name)")] string LiveMessage
        )
        {
            await DeferAsync(ephemeral: true);

            Uri? ProfileURL = null;
            ITextChannel? WhereToPost = null;
            IRole? RoleToMention = null;
            var AutoChannel = false;

            /* Parse for a profile url that was included (if any) */
            ProfileURL = UriUtils.FindFirstUri(LiveMessage);
            if (ProfileURL == null)
            {
                await FollowupAsync(text: "Could not automatically parse a Profile URL from your message. Please try again", ephemeral: true);
                return;
            }
            var monitor = GetMonitor(ProfileURL);
            LiveMessage = LiveMessage.Replace(ProfileURL.AbsoluteUri, "{url}", StringComparison.InvariantCultureIgnoreCase);

            /* Parse for a channel that was mentioned (if any) */
            var guildChannels = await Context.Guild.GetTextChannelsAsync();
            WhereToPost = guildChannels.Where(x => LiveMessage.Contains(x.Mention, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault<ITextChannel>();
            if (WhereToPost == null)
            {
                AutoChannel = true;
                WhereToPost = await Context.Guild.GetTextChannelAsync(Context.Channel.Id);
            }
            LiveMessage = LiveMessage.Replace(WhereToPost.Mention, "", StringComparison.InvariantCultureIgnoreCase);

            /* Parse for a role that was mentioned (if any) */
            var mentionedRole = Context.Guild.Roles.Where(x => LiveMessage.Contains(x.Mention, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault<IRole>();
            if (mentionedRole != null)
            {
                RoleToMention = mentionedRole;
                LiveMessage = LiveMessage.Replace(mentionedRole.Mention, "{role}", StringComparison.InvariantCultureIgnoreCase);
            }

            LiveMessage = LiveMessage.Trim();

            var allowedMentions = new AllowedMentions()
            {
                AllowedTypes = AllowedMentionTypes.None
            };

            var subscription = await SetupSubscription(monitor: monitor, uri: ProfileURL, message: LiveMessage, guild: Context.Guild, channel: WhereToPost, role: RoleToMention);
            var ResponseMessage = $"Success! I will post in {WhereToPost.Mention} when {Format.Bold(subscription.User.DisplayName)} with a live message of {Format.Code(subscription.Message)} and mentioning {RoleToMention?.Mention ?? "nobody"}\n";

            if (AutoChannel)
            {
                var WarningEmoji = new Emoji("\u26A0");
                ResponseMessage += @$"
{WarningEmoji} Warning: Couldn't detect a channel automatically from your message, so I chose the one you're currently in.
To change this, please run the {Format.Code("/monitor edit")} command
";
            }

            await FollowupAsync(text: ResponseMessage, ephemeral: true, allowedMentions: allowedMentions);
        }

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
            [Summary(name: "live-message", description: "This message will be sent out when the streamer goes live (check help for more info)")] string LiveMessage,
            [Summary(name: "role-to-mention", description: "The role to replace {role} with in the live message (default is none)")] IRole? RoleToMention = null
        )
        {
            await DeferAsync(ephemeral: true);

            var monitor = GetMonitor(ProfileURL);

            LiveMessage = LiveMessage.Trim();

            var allowedMentions = new AllowedMentions()
            {
                AllowedTypes = AllowedMentionTypes.None
            };

            var subscription = await SetupSubscription(monitor: monitor, uri: ProfileURL, message: LiveMessage, guild: Context.Guild, channel: WhereToPost, role: RoleToMention);
            var ResponseMessage = $"Success! I will post in {WhereToPost.Mention} when {Format.Bold(subscription.User.DisplayName)} with a live message of {Format.Code(subscription.Message)} and mentioning {RoleToMention?.Mention ?? "nobody"}\n";

            if (RoleToMention != null && !LiveMessage.Contains("{role}", StringComparison.InvariantCultureIgnoreCase))
            {
                var WarningEmoji = new Emoji("\u26A0");
                ResponseMessage += @$"

{WarningEmoji} Warning: Looks like you want to mention {RoleToMention.Mention}, but didn't put the placeholder {Format.Code("{role}")} in your live message.
With this current setup, nobody will be pinged. If this is what you would like, no further modifications are required.
If you would like to actually ping {RoleToMention?.Mention}, please run the following command:
{Format.Code($"/monitor edit profile-url: {ProfileURL} live-message: {"{role} " + LiveMessage}")}
";
            }
            await FollowupAsync(text: ResponseMessage, ephemeral: true, allowedMentions: allowedMentions);
        }

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
            [Summary(name: "live-message", description: "This message will be sent out when the streamer goes live (check help for more info)")] string? LiveMessage = null,
            [Summary(name: "role-to-mention", description: "The role to replace {role} with in the live message (default is none)")] IRole? RoleToMention = null
        )
        {
            await DeferAsync(ephemeral: true);

            var CompletedMessage = "";
            if (WhereToPost == null && LiveMessage == null && RoleToMention == null)
                CompletedMessage = $"Nothing was updated for {Format.EscapeUrl(ProfileURL.AbsoluteUri)}";
            if (WhereToPost != null)
                CompletedMessage += $"Updated channel to {WhereToPost.Mention}\n";
            if (RoleToMention != null)
                CompletedMessage += $"Updated role to {RoleToMention.Mention}\n";
            if (LiveMessage != null)
                CompletedMessage += $"Updated message to {Format.Code(LiveMessage)}\n";

            var allowedMentions = new AllowedMentions()
            {
                AllowedTypes = AllowedMentionTypes.None
            };
            await FollowupAsync(CompletedMessage, ephemeral: true, allowedMentions: allowedMentions);
        }

        /// <summary>
        /// Delete a stream monitor
        /// </summary>
        /// <param name="ProfileURL"></param>
        /// <returns></returns>
        [RequireBotManager]
        [SlashCommand(name: "delete", description: "Delete a stream from being monitored")]
        public async Task DeleteStreamMonitor(
            [Summary(name: "profile-url", description: "The profile page of the streamer")] Uri ProfileURL
        )
        {
            await DeferAsync(ephemeral: true);

            await FollowupAsync($"Deleted monitor for {Format.EscapeUrl(ProfileURL.AbsoluteUri)}", ephemeral: true);
        }

        /// <summary>
        /// List all stream monitors in the Guild
        /// </summary>
        /// <returns></returns>
        [SlashCommand(name: "list", description: "List all stream monitors")]
        public async Task ListStreamMonitor(Uri uri)
        {
            await DeferAsync(ephemeral: true);

            var monitor = GetMonitor(uri);
            var livebotUser = await monitor.GetUser(profileURL: uri.AbsoluteUri);
            var user = await _work.UserRepository.SingleOrDefaultAsync(x => x.SourceID == livebotUser.Id && x.ServiceType == livebotUser.ServiceType);
            await FollowupAsync($"Successfully retrieved {user.DisplayName} from the database with an ID of {user.SourceID} on {user.ServiceType}", ephemeral: true);
        }

        [SlashCommand(name: "help", description: "Get some help with setting up a stream monitor")]
        public async Task HelpStreamMonitor()
        {
            var message = @$"
With the {Format.Code("live-message")} portion of the commands, you can use the below placeholders (including the {Format.Code("{")} and {Format.Code("}")}.
Using these placeholders {Format.Bold("AS IS")} will {Format.Bold("AUTOMATICALLY")} replace them when the bot sends a message

Example:
{Format.Code("{role} {name} is live and playing {game}! {url}")}

**will AUTOMATICALLY turn into**

{Format.Code("@Live bsquidwrd is live and playing Valorant! https://twitch.tv/bsquidwrd")}

{Format.Code("role")} = The role that you setup to be pinged
{Format.Code("name")} = The display name of the Streamer
{Format.Code("game")} = Game at the time of going live
{Format.Code("url")} = Link to the stream
{Format.Code("title")} = Stream title at time of going live

You can find a full guide here: {Format.EscapeUrl("https://bsquidwrd.gitbook.io/livebot-docs/tutorial-walkthrough/start-monitoring-a-stream")}
";
            await RespondAsync(message, ephemeral: true);
        }

        private async Task<StreamSubscription> SetupSubscription(ILiveBotMonitor monitor, Uri uri, string message, IGuild guild, ITextChannel channel, IRole? role = null)
        {
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(x => x.DiscordId == guild.Id);
            var discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(x => x.DiscordId == channel.Id);
            DiscordRole? discordRole = null;
            if (role != null)
                discordRole = await _work.RoleRepository.SingleOrDefaultAsync(x => x.DiscordId == role.Id);

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
            await _work.UserRepository.AddOrUpdateAsync(streamUser, (i => i.ServiceType == monitorUser.ServiceType && i.SourceID == monitorUser.Id));
            streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == monitorUser.ServiceType && i.SourceID == monitorUser.Id);

            Expression<Func<StreamSubscription, bool>> streamSubscriptionPredicate = (i =>
                i.User == streamUser &&
                i.DiscordGuild == discordGuild
            );

            StreamSubscription newSubscription = new()
            {
                User = streamUser,
                DiscordGuild = discordGuild,
                DiscordChannel = discordChannel,
                DiscordRole = discordRole,
                Message = message
            };

            await _work.SubscriptionRepository.AddOrUpdateAsync(newSubscription, streamSubscriptionPredicate);

            return await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);
        }
    }
}