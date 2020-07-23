using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using LiveBot.Discord.Helpers;
using LiveBot.Discord.Helpers.RuntimeResults;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [Group("monitor")]
    [Summary("Monitor actions for Streams")]
    public class MonitorModule : InteractiveBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;
        private readonly IEnumerable<ILiveBotMonitor> _monitors;

        /// <summary>
        /// Represents the list of Monitoring Commands available
        /// </summary>
        /// <param name="monitors">
        /// The loaded Monitoring Services, later used for locating and processing requests
        /// </param>
        /// <param name="factory">The database factory so that the database can be utilized</param>
        public MonitorModule(IEnumerable<ILiveBotMonitor> monitors, IUnitOfWorkFactory factory)
        {
            _monitors = monitors;
            _work = factory.Create();
        }

        #region Owner Commands

        /// <summary>
        /// Gives a list of loaded Monitoring Services
        /// </summary>
        [Command("services", RunMode = RunMode.Async)]
        [RequireOwner]
        [Summary("Get the list of Stream Services that are loaded")]
        public async Task GetMonitorServices()
        {
            await ReplyAsync($"Loaded Services: {string.Join(",", _monitors.Select(i => i.ServiceType).Distinct())}");
        }

        /// <summary>
        /// Checks if a particular Monitoring Service is loaded with a given string name (based on <c>ServiceEnum</c>
        /// </summary>
        /// <param name="serviceName"></param>
        [Command("services", RunMode = RunMode.Async)]
        [RequireOwner]
        [Summary("Check if a particular Stream Service is loaded by name")]
        public async Task GetMonitorService(string serviceName)
        {
            ServiceEnum serviceEnum = (ServiceEnum)Enum.Parse(typeof(ServiceEnum), serviceName.ToUpper());
            ILiveBotMonitor monitor = _monitors.Where(m => m.ServiceType == serviceEnum).First();
            await ReplyAsync($"Loaded: {monitor.ServiceType}");
        }

        #endregion Owner Commands

        #region Misc Commands

        /// <summary>
        /// Runs a test of permissions for the given <c>Context</c>
        /// </summary>
        [Command("test", RunMode = RunMode.Async)]
        [Alias("perms")]
        [Summary(@"
Have the bot perform a self check of its required Discord permissions in the channel the command is run.
Don't worry, this won't send any weird messages. It will only send a response with the result.
")]
        public async Task MonitorTest()
        {
            var guildUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var guildChannel = Context.Guild.GetChannel(Context.Channel.Id);
            var guildUserChannelPerms = guildUser.GetPermissions(guildChannel);

            List<string> missingPermissions = new List<string>();

            if (!guildUserChannelPerms.SendMessages)
            {
                return;
            }

            if (!guildUserChannelPerms.EmbedLinks)
            {
                missingPermissions.Add("Embed Links");
            }

            if (!guildUserChannelPerms.AttachFiles)
            {
                missingPermissions.Add("Attach Files");
            }

            if (!guildUserChannelPerms.MentionEveryone)
            {
                missingPermissions.Add("Mention Everyone");
            }

            string permissionsResult;
            if (missingPermissions.Count == 0)
            {
                permissionsResult = "All Set!";
            }
            else
            {
                permissionsResult = $"Missing Permissions: {string.Join(", ", missingPermissions)}";
            }
            await ReplyAsync($"{Context.Message.Author.Mention}, {permissionsResult}");
        }

        /// <summary>
        /// Gives a list of Streams being monitored for the Discord Service it is run in
        /// </summary>
        [Command("list", RunMode = RunMode.Async)]
        [Summary("Get a list of all Streams being monitored for the Discord Server")]
        public async Task MonitorList()
        {
            int pageSize = 5;

            Expression<Func<StreamSubscription, bool>> streamSubscriptionPredicate = (i =>
                i.DiscordGuild.DiscordId == Context.Guild.Id
            );
            int pageCount = await _work.SubscriptionRepository.GetPageCountAsync(streamSubscriptionPredicate, pageSize);
            List<EmbedFieldBuilder> subscriptions = new List<EmbedFieldBuilder>();

            for (int i = 1; i < pageCount + 1; i++)
            {
                var streamSubscriptions = await _work.SubscriptionRepository.FindAsync(streamSubscriptionPredicate, i, pageSize);
                if (streamSubscriptions.Count() == 0)
                    continue;
                foreach (StreamSubscription streamSubscription in streamSubscriptions)
                {
                    string fieldValue = "";
                    fieldValue += $"Channel: {MentionUtils.MentionChannel(streamSubscription.DiscordChannel.DiscordId)}\n";
                    fieldValue += $"Role: {streamSubscription.DiscordRole?.Name?.Replace("@everyone", "everyone") ?? "none"}\n";
                    fieldValue += $"Message: {streamSubscription.Message}";
                    EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                        .WithIsInline(false)
                        .WithName(streamSubscription.User.ProfileURL)
                        .WithValue(fieldValue);
                    subscriptions.Add(fieldBuilder);
                }
            }

            PaginatedMessage paginatedMessage = new PaginatedMessage
            {
                Color = Color.LightGrey,
                Title = "Stream Subscriptions",
                Pages = subscriptions
            };
            await PagedReplyAsync(paginatedMessage);
        }

        /// <summary>
        /// Checks if a given stream is live, if so returns default notification message and embed
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [Command("check", RunMode = RunMode.Async)]
        [Summary("Check if Stream is live. Basic debug command to see if the Bot can locate a stream")]
        public async Task CheckStream(ILiveBotUser user)
        {
            ILiveBotMonitor monitor = _GetServiceMonitor(user);
            ILiveBotStream stream = await monitor.GetStream_Force(user);
            if (stream == null)
            {
                await ReplyAsync($"Doesn't look like the user {user.DisplayName} is live on {user.ServiceType}");
                return;
            }
            Embed streamEmbed = NotificationHelpers.GetStreamEmbed(stream: stream, user: stream.User, game: stream.Game);
            StreamSubscription bogusSubscription = new StreamSubscription() { Message = Defaults.NotificationMessage };
            string notificationMessage = NotificationHelpers.GetNotificationMessage(stream, bogusSubscription);
            await ReplyAsync(message: $"Service: `{stream.ServiceType}`\n{notificationMessage}", embed: streamEmbed);
        }

        #endregion Misc Commands

        #region Start Commands

        /// <summary>
        /// Runs through the process of Starting a Stream from being monitored
        /// </summary>
        [Command("start", RunMode = RunMode.Async)]
        [Alias("edit", "add")]
        [Summary("Setup a new Stream to monitor for this Discord")]
        public async Task<RuntimeResult> MonitorStart_RequestURL()
        {
            string profileURL = await _RequestStreamUser();
            if (profileURL == null)
                return MonitorResult.FromError($"${Context.Message.Author.Mention}, Please provide a valid Stream URL");
            ILiveBotMonitor monitor = _GetServiceMonitor(profileURL);
            ILiveBotUser user = await monitor.GetUser(profileURL: profileURL);
            return await MonitorStart(user);
        }

        /// <summary>
        /// Runs through the process of Starting a Stream from being monitored
        /// </summary>
        /// <param name="user"></param>
        [Command("start", RunMode = RunMode.Async)]
        [Alias("edit", "add")]
        [Summary("Setup a new Stream to monitor for this Discord")]
        public async Task<RuntimeResult> MonitorStart(ILiveBotUser user)
        {
            ILiveBotMonitor monitor = _GetServiceMonitor(user);

            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(g => g.DiscordId == Context.Guild.Id);
            if (discordGuild == null)
            {
                DiscordGuild newDiscordGuild = new DiscordGuild
                {
                    DiscordId = Context.Guild.Id,
                    Name = Context.Guild.Name,
                    IconUrl = Context.Guild.IconUrl
                };
                await _work.GuildRepository.AddOrUpdateAsync(newDiscordGuild, g => g.DiscordId == Context.Guild.Id);
                discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(g => g.DiscordId == Context.Guild.Id);
            }

            // Get Notification Channel
            DiscordChannel discordChannel = await _RequestNotificationChannel(discordGuild);
            if (discordChannel == null)
                return MonitorResult.FromError($"{Context.Message.Author.Mention}, Please re-run the command and be sure to mention a channel.");

            // Get Notification Message
            string notificationMessage = await _RequestNotificationMessage();
            if (notificationMessage == null)
                return MonitorResult.FromError($"{Context.Message.Author.Mention}, Please re-run the command and provide a valid message for notifications");

            // Get Notification Role
            DiscordRole discordRole = await _RequestNotificationRole(discordGuild);

            // Process their answers
            StreamUser streamUser = new StreamUser()
            {
                ServiceType = user.ServiceType,
                SourceID = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarURL = user.AvatarURL,
                ProfileURL = user.ProfileURL
            };
            await _work.UserRepository.AddOrUpdateAsync(streamUser, (i => i.ServiceType == user.ServiceType && i.SourceID == user.Id));
            streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == user.ServiceType && i.SourceID == user.Id);

            Expression<Func<StreamSubscription, bool>> streamSubscriptionPredicate = (i =>
                i.User == streamUser &&
                i.DiscordGuild == discordGuild
            );

            StreamSubscription newSubscription = new StreamSubscription()
            {
                User = streamUser,
                DiscordGuild = discordGuild,
                DiscordChannel = discordChannel,
                DiscordRole = discordRole,
                Message = notificationMessage
            };

            try
            {
                StreamSubscription existingSubscription = await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);

                if (existingSubscription != null)
                {
                    existingSubscription.DiscordGuild = newSubscription.DiscordGuild;
                    existingSubscription.DiscordChannel = newSubscription.DiscordChannel;
                    existingSubscription.DiscordRole = newSubscription.DiscordRole;
                    existingSubscription.Message = newSubscription.Message;
                    await _work.SubscriptionRepository.UpdateAsync(existingSubscription);
                }
                else
                {
                    await _work.SubscriptionRepository.AddOrUpdateAsync(newSubscription, streamSubscriptionPredicate);
                }

                StreamSubscription streamSubscription = await _work.SubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);

                monitor.AddChannel(user);
                string escapedMessage = NotificationHelpers.EscapeSpecialDiscordCharacters(streamSubscription.Message);
                await ReplyAsync($"{Context.Message.Author.Mention}, I have setup a subscription for {user.DisplayName} on {user.ServiceType} with message {escapedMessage}");

                return MonitorResult.FromSuccess();
            }
            catch (Exception e)
            {
                Log.Error($"Error running MonitorStart for {Context.Message.Author.Id} {Context.Message.Author.Username}#{Context.Message.Author.Discriminator} GuildID: {Context.Guild.Id} ChannelID: {Context.Channel.Id}\n{e}");
                return MonitorResult.FromError($"{Context.Message.Author.Mention}, I wasn't able to create a subscription for the user {user.DisplayName} on {user.ServiceType}. Please try again or contact my owner");
            }
        }

        #endregion Start Commands

        #region Stop Commands

        /// <summary>
        /// Runs through the process of Stopping a Stream from being monitored
        /// </summary>
        [Command("stop", RunMode = RunMode.Async)]
        [Alias("end", "remove")]
        [Summary("Stop monitoring a Stream for this Discord")]
        public async Task<RuntimeResult> MonitorStop_RequestURL()
        {
            string profileURL = await _RequestStreamUser();
            if (profileURL == null)
                return MonitorResult.FromError($"{Context.Message.Author.Mention}, Please provide a valid Stream URL for me to stop monitoring");
            ILiveBotMonitor monitor = _GetServiceMonitor(profileURL);
            ILiveBotUser user = await monitor.GetUser(profileURL: profileURL);
            return await MonitorStop(user);
        }

        /// <summary>
        /// Runs through the process of Stopping a Stream from being monitored
        /// </summary>
        /// <param name="user"></param>
        [Command("stop", RunMode = RunMode.Async)]
        [Alias("end", "remove")]
        [Summary("Stop monitoring a Stream for this Discord")]
        public async Task<RuntimeResult> MonitorStop(ILiveBotUser user)
        {
            ILiveBotMonitor monitor = _GetServiceMonitor(user);

            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(g => g.DiscordId == Context.Guild.Id);
            if (discordGuild == null)
            {
                DiscordGuild newDiscordGuild = new DiscordGuild
                {
                    DiscordId = Context.Guild.Id,
                    Name = Context.Guild.Name,
                    IconUrl = Context.Guild.IconUrl
                };
                await _work.GuildRepository.AddOrUpdateAsync(newDiscordGuild, g => g.DiscordId == Context.Guild.Id);
                discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(g => g.DiscordId == Context.Guild.Id);
            }

            StreamUser streamUser = new StreamUser()
            {
                ServiceType = user.ServiceType,
                SourceID = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarURL = user.AvatarURL,
                ProfileURL = user.ProfileURL
            };
            await _work.UserRepository.AddOrUpdateAsync(streamUser, (i => i.ServiceType == user.ServiceType && i.SourceID == user.Id));
            streamUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == user.ServiceType && i.SourceID == user.Id);

            Expression<Func<StreamSubscription, bool>> streamSubscriptionPredicate = (i =>
                i.User == streamUser &&
                i.DiscordGuild == discordGuild
            );
            IEnumerable<StreamSubscription> streamSubscriptions = await _work.SubscriptionRepository.FindAsync(streamSubscriptionPredicate);
            try
            {
                foreach (StreamSubscription streamSubscription in streamSubscriptions)
                {
                    await _work.SubscriptionRepository.RemoveAsync(streamSubscription.Id);
                }
                await ReplyAsync($"{Context.Message.Author.Mention}, I have removed the Subscription for {user.DisplayName}");
                return MonitorResult.FromSuccess();
            }
            catch (Exception e)
            {
                Log.Error($"Error running MonitorStop for {Context.Message.Author.Id} {Context.Message.Author.Username}#{Context.Message.Author.Discriminator} GuildID: {Context.Guild.Id} ChannelID: {Context.Channel.Id}\n{e}");
                return MonitorResult.FromSuccess($"{Context.Message.Author.Mention}, I couldn't remove the Subscription for user {user.DisplayName}. Please try again or contact my owner");
            }
        }

        #endregion Stop Commands

        #region Helper Functions

        /// <summary>
        /// Simple method to delete a message and not fail
        /// </summary>
        /// <param name="message"></param>
        private async Task _DeleteMessage(IMessage message)
        {
            try
            {
                await message.DeleteAsync();
            }
            catch
            { }
        }

        /// <summary>
        /// Attempts to locate the loaded Monitoring Service for the given
        /// <c>ILiveBotBase</c><paramref name="obj"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>ILiveBotMonitor object which represents a loaded Monitoring Service</returns>
        private ILiveBotMonitor _GetServiceMonitor(ILiveBotBase obj)
        {
            return _monitors.Where(o => o.ServiceType == obj.ServiceType).First();
        }

        /// <summary>
        /// Attempts to locate the loaded Monitoring Service for the give <paramref name="streamURL"/>
        /// </summary>
        /// <param name="streamURL"></param>
        /// <returns>ILiveBotMonitor object which represents a loaded Monitoring Service</returns>
        private ILiveBotMonitor _GetServiceMonitor(string streamURL)
        {
            return _monitors.Where(o => o.IsValid(streamURL)).First();
        }

        /// <summary>
        /// Prompts the user to enter a Stream URL
        /// </summary>
        /// <returns></returns>
        private async Task<string> _RequestStreamUser()
        {
            var messageEmbedFooter = new EmbedFooterBuilder()
                .WithText("Example: https://twitch.tv/bsquidwrd");
            var messageEmbed = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithDescription("What is the URL of the stream?")
                .WithFooter(messageEmbedFooter)
                .Build();

            var questionMessage = await ReplyAsync(message: $"{Context.Message.Author.Mention}", embed: messageEmbed);
            var responseMessage = await NextMessageAsync(timeout: Defaults.MessageTimeout);

            string returnValue = responseMessage?.Content;

            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);
            return returnValue;
        }

        /// <summary>
        /// Helper Function to simplify asking for a channel
        /// </summary>
        /// <returns>Resulting Channel object from the Users input</returns>
        private async Task<DiscordChannel> _RequestNotificationChannel(DiscordGuild discordGuild)
        {
            var messageEmbedFooter = new EmbedFooterBuilder()
                .WithText("Please mention the channel with the # prefix");
            var messageEmbed = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithDescription($"Please mention the Discord Channel you would like to start or stop a notification for.\nEx: {MentionUtils.MentionChannel(Context.Channel.Id)}")
                .WithFooter(messageEmbedFooter)
                .Build();

            var questionMessage = await ReplyAsync(message: $"{Context.Message.Author.Mention}", embed: messageEmbed);
            var responseMessage = await NextMessageAsync(timeout: Defaults.MessageTimeout);

            IGuildChannel guildChannel = responseMessage.MentionedChannels.FirstOrDefault();
            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);

            DiscordChannel discordChannel = null;
            if (guildChannel != null)
            {
                discordChannel = new DiscordChannel
                {
                    DiscordGuild = discordGuild,
                    DiscordId = guildChannel.Id,
                    Name = guildChannel.Name
                };
                await _work.ChannelRepository.AddOrUpdateAsync(discordChannel, i => i.DiscordGuild == discordGuild && i.DiscordId == guildChannel.Id);
                discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(c => c.DiscordId == guildChannel.Id);
            }

            return discordChannel;
        }

        /// <summary>
        /// Helper Function to simplify asking for a Role to mention
        /// </summary>
        /// <returns></returns>
        private async Task<DiscordRole> _RequestNotificationRole(DiscordGuild discordGuild)
        {
            var messageEmbedFooter = new EmbedFooterBuilder()
                .WithText("You do NOT have to mention the role with the @ symbol");
            var messageEmbed = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithDescription($"What is the name of the Role you would like to mention in messages?\nEx: `{Context.Guild.CurrentUser.Roles.First(d => d.IsEveryone == false).Name}`, `everyone` or `none` if you don't want to ping a role")
                .WithFooter(messageEmbedFooter)
                .Build();

            var questionMessage = await ReplyAsync(message: $"{Context.Message.Author.Mention}", embed: messageEmbed);
            var responseMessage = await NextMessageAsync(timeout: Defaults.MessageTimeout);
            IRole role = responseMessage.MentionedRoles.FirstOrDefault();
            if (role == null)
            {
                string response = responseMessage.Content.Trim();
                if (response.Equals("everyone", StringComparison.CurrentCultureIgnoreCase))
                {
                    role = Context.Guild.EveryoneRole;
                }
                else if (response.Equals("none", StringComparison.CurrentCultureIgnoreCase))
                {
                    role = null;
                }
                else
                {
                    role = Context.Guild.Roles.FirstOrDefault(d => d.Name.Equals(response, StringComparison.CurrentCultureIgnoreCase));
                }
            }
            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);

            DiscordRole discordRole = null;
            if (role != null)
            {
                discordRole = new DiscordRole
                {
                    DiscordGuild = discordGuild,
                    DiscordId = role.Id,
                    Name = role.Name
                };
                await _work.RoleRepository.AddOrUpdateAsync(discordRole, i => i.DiscordGuild == discordGuild && i.DiscordId == role.Id);
                discordRole = await _work.RoleRepository.SingleOrDefaultAsync(r => r.DiscordId == role.Id);
            }

            return discordRole;
        }

        private async Task<string> _RequestNotificationMessage()
        {
            string parameters = "";
            parameters += "{role} - Role to ping (if applicable)\n";
            parameters += "{name} - Streamers Name\n";
            parameters += "{game} - Game they are playing\n";
            parameters += "{url} - URL to the stream\n";
            parameters += "{title} - Stream Title\n";

            var embedFieldBuilder = new EmbedFieldBuilder()
                .WithIsInline(true)
                .WithName("Parameters")
                .WithValue(parameters);

            var messageEmbedFooter = new EmbedFooterBuilder()
                .WithText($"Default: {Defaults.NotificationMessage}");
            var messageEmbed = new EmbedBuilder()
                .WithColor(Color.DarkPurple)
                .WithDescription("What message should be sent? (Max 255 characters)\nIf you'd like to use the default (see footer) type default")
                .WithFields(embedFieldBuilder)
                .WithFooter(messageEmbedFooter)
                .Build();

            var questionMessage = await ReplyAsync(message: $"{Context.Message.Author.Mention}", embed: messageEmbed);
            var responseMessage = await NextMessageAsync(timeout: Defaults.MessageTimeout);
            string notificationMessage = responseMessage.Content.Trim();

            if (notificationMessage.Equals("default", StringComparison.CurrentCultureIgnoreCase))
            {
                notificationMessage = Defaults.NotificationMessage;
            }

            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);
            return notificationMessage;
        }

        #endregion Helper Functions
    }
}