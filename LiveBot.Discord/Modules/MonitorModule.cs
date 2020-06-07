using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using LiveBot.Discord.Helpers;
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
        private readonly List<ILiveBotMonitor> _monitors;

        /// <summary>
        /// Represents the list of Monitoring Commands available
        /// </summary>
        /// <param name="monitors">The loaded Monitoring Services, later used for locating and processing requests</param>
        /// <param name="factory">The database factory so that the database can be utilized</param>
        public MonitorModule(List<ILiveBotMonitor> monitors, IUnitOfWorkFactory factory)
        {
            _monitors = monitors;
            _work = factory.Create();
        }

        #region Owner Commands

        /// <summary>
        /// Gives a list of loaded Monitoring Services
        /// </summary>
        [Command("services")]
        [RequireOwner]
        [Summary("Get the list of Stream Services that are loaded")]
        public async Task MonitorServices()
        {
            List<string> loadedServices = new List<string>();
            foreach (var monitor in _monitors)
            {
                string monitorName = monitor.ServiceType.ToString();
                loadedServices.Add(monitorName);
            }
            await ReplyAsync($"Loaded Services: {string.Join(",", loadedServices)}");
        }

        /// <summary>
        /// Checks if a particular Monitoring Service is loaded with a given string name (based on <c>ServiceEnum</c>
        /// </summary>
        /// <param name="serviceName"></param>
        [Command("services")]
        [RequireOwner]
        [Summary("Check if a particular Stream Service is loaded by name")]
        public async Task MonitorServices(string serviceName)
        {
            ServiceEnum serviceEnum = (ServiceEnum)Enum.Parse(typeof(ServiceEnum), serviceName.ToUpper());
            ILiveBotMonitor monitor = _monitors.Where(m => m.ServiceType == serviceEnum).First();
            await ReplyAsync($"Loaded: {monitor.ServiceType.ToString()}");
        }

        #endregion Owner Commands

        #region Misc Commands

        /// <summary>
        /// Runs a test of permissions for the given <c>Context</c>
        /// </summary>
        [Command("test")]
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
                Log.Debug("All required permissions are set");
                permissionsResult = "All Set!";
            }
            else
            {
                Log.Debug("Missing some permissions");
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
            // TODO: Implement MonitorList (PagedReplyAsync)
            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((g => g.DiscordId == Context.Guild.Id));
            var streamSubscriptions = await _work.StreamSubscriptionRepository.FindAsync(i => i.DiscordChannel.DiscordGuild == discordGuild);

            string combinedStreams = "";
            combinedStreams = string.Join("\n", streamSubscriptions.Select(i => i.User.SourceID));
            foreach (StreamSubscription streamSubscription in streamSubscriptions)
            {
                combinedStreams += $"{streamSubscription.User.SourceID}: {streamSubscription.Message} {streamSubscription.User.ServiceType}\n";
            }
            await ReplyAsync($"Stream Subscriptions for this Server:\n{combinedStreams}");
        }

        /// <summary>
        /// Checks if a given stream is live, if so returns default notification message and embed
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [Command("check")]
        [RequireOwner]
        [Summary("Check if Stream is live. Basic debug command to see if the Bot can locate a stream")]
        public async Task CheckStream(ILiveBotUser user)
        {
            ILiveBotMonitor monitor = _GetServiceMonitor(user);
            ILiveBotStream stream = await monitor.GetStream(user);
            if (stream == null)
            {
                await ReplyAsync($"{user.ServiceName} Doesn't look like the user {user.DisplayName} is live");
                return;
            }
            Embed streamEmbed = NotificationHelpers.GetStreamEmbed(stream);
            StreamSubscription bogusSubscription = new StreamSubscription() { Message = Defaults.NotificationMessage };
            string notificationMessage = NotificationHelpers.GetNotificationMessage(stream, bogusSubscription);
            await ReplyAsync(message: $"Service: `{stream.ServiceName}`\n{notificationMessage}", embed: streamEmbed);
        }

        #endregion Misc Commands

        #region Start Commands

        /// <summary>
        /// Runs through the process of Starting a Stream from being monitored
        /// </summary>
        [Command("start", RunMode = RunMode.Async)]
        [Alias("edit", "add")]
        [Summary("Setup a new Stream to monitor for this Discord")]
        public async Task MonitorStart_RequestURL()
        {
            string profileURL = await _RequestStreamUser();
            if (profileURL == null)
                return;
            ILiveBotMonitor monitor = _GetServiceMonitor(profileURL);
            ILiveBotUser user = await monitor.GetUser(profileURL: profileURL);
            await MonitorStart(user);
        }

        /// <summary>
        /// Runs through the process of Starting a Stream from being monitored
        /// </summary>
        /// <param name="user"></param>
        [Command("start", RunMode = RunMode.Async)]
        [Alias("edit", "add")]
        [Summary("Setup a new Stream to monitor for this Discord")]
        public async Task MonitorStart(ILiveBotUser user)
        {
            ILiveBotMonitor monitor = _GetServiceMonitor(user);

            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(g => g.DiscordId == Context.Guild.Id);

            // Get Notification Channel
            IGuildChannel notificationChannel = await _RequestNotificationChannel();
            DiscordChannel discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(c => c.DiscordId == notificationChannel.Id);

            // Get Notification Message
            string notificationMessage = await _RequestNotificationMessage();

            // Get Notification Role
            IRole mentionRole = await _RequestNotificationRole();
            DiscordRole discordRole = await _work.RoleRepository.SingleOrDefaultAsync(r => r.DiscordId == mentionRole.Id);

            // Process their answers
            StreamUser streamUser = new StreamUser()
            {
                ServiceType = user.ServiceType,
                SourceID = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarURL = user.AvatarURL,
                ProfileURL = user.GetProfileURL()
            };
            await _work.StreamUserRepository.AddOrUpdateAsync(streamUser, (i => i.ServiceType == user.ServiceType && i.SourceID == user.Id));
            streamUser = await _work.StreamUserRepository.SingleOrDefaultAsync(i => i.ServiceType == user.ServiceType && i.SourceID == user.Id);

            Expression<Func<StreamSubscription, bool>> streamSubscriptionPredicate = (i =>
                i.User == streamUser &&
                i.DiscordChannel.DiscordGuild == discordGuild
            );

            StreamSubscription streamSubscription = new StreamSubscription()
            {
                User = streamUser,
                DiscordChannel = discordChannel,
                DiscordRole = discordRole,
                Message = notificationMessage
            };
            await _work.StreamSubscriptionRepository.AddOrUpdateAsync(streamSubscription, streamSubscriptionPredicate);
            streamSubscription = await _work.StreamSubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);

            if (streamSubscription != null)
            {
                string escapedMessage = NotificationHelpers.EscapeSpecialDiscordCharacters(streamSubscription.Message);
                monitor.AddChannel(user);
                await ReplyAsync($"{Context.Message.Author.Mention}, I have setup a subscription for {user.DisplayName} on {user.ServiceType} with message {escapedMessage}");
            }
            else
            {
                try
                {
                    await _work.StreamSubscriptionRepository.RemoveAsync(streamSubscription.Id);
                }
                catch { }
                await ReplyAsync($"{Context.Message.Author.Mention}, I wasn't able to create a subscription for the user {user.DisplayName} on {user.ServiceType}. Please try again or contact my owner");
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
        public async Task MonitorStop_RequestURL()
        {
            string profileURL = await _RequestStreamUser();
            if (profileURL == null)
                return;
            ILiveBotMonitor monitor = _GetServiceMonitor(profileURL);
            ILiveBotUser user = await monitor.GetUser(profileURL: profileURL);
            await MonitorStop(user);
        }

        /// <summary>
        /// Runs through the process of Stopping a Stream from being monitored
        /// </summary>
        /// <param name="user"></param>
        [Command("stop", RunMode = RunMode.Async)]
        [Alias("end")]
        [Summary("Stop monitoring a Stream for this Discord")]
        public async Task MonitorStop(ILiveBotUser user)
        {
            // TODO: Implement Monitor Stop
            ILiveBotMonitor monitor = _GetServiceMonitor(user);

            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(g => g.DiscordId == Context.Guild.Id);

            StreamUser streamUser = new StreamUser()
            {
                ServiceType = user.ServiceType,
                SourceID = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarURL = user.AvatarURL,
                ProfileURL = user.GetProfileURL()
            };
            await _work.StreamUserRepository.AddOrUpdateAsync(streamUser, (i => i.ServiceType == user.ServiceType && i.SourceID == user.Id));
            streamUser = await _work.StreamUserRepository.SingleOrDefaultAsync(i => i.ServiceType == user.ServiceType && i.SourceID == user.Id);

            Expression<Func<StreamSubscription, bool>> streamSubscriptionPredicate = (i =>
                i.User == streamUser &&
                i.DiscordChannel.DiscordGuild == discordGuild
            );
            StreamSubscription streamSubscription = await _work.StreamSubscriptionRepository.SingleOrDefaultAsync(streamSubscriptionPredicate);
            try
            {
                monitor.RemoveChannel(user);
                await _work.StreamSubscriptionRepository.RemoveAsync(streamSubscription.Id);
                await ReplyAsync($"{Context.Message.Author.Mention} This command isn't implemented yet, but you input {user.DisplayName} in service {monitor.ServiceType}");
            }
            catch
            {
                await ReplyAsync($"{Context.Message.Author.Mention}, I couldn't remove the Subscription for user {user.DisplayName}. Please try again or contact my owner");
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
        /// Attempts to locate the loaded Monitoring Service for the given <c>ILiveBotBase</c> <paramref name="obj"/>
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
            await ReplyAsync($"{Context.Message.Author.Mention}, What is the URL of the stream?");
            var response = await NextMessageAsync(timeout: Defaults.MessageTimeout);

            if (response != null)
            {
                return response.Content;
            }
            else
            {
                await ReplyAsync("You did not reply before the timeout");
                return null;
            }
        }

        /// <summary>
        /// Helper Function to simplify asking for a channel
        /// </summary>
        /// <returns>Resulting Channel object from the Users input</returns>
        private async Task<IGuildChannel> _RequestNotificationChannel()
        {
            var questionMessage = await ReplyAsync($"{Context.Message.Author.Mention}, Please mention the Discord Channel you would like to start or stop a notification for. Ex: {MentionUtils.MentionChannel(Context.Channel.Id)}");
            var responseMessage = await NextMessageAsync(timeout: Defaults.MessageTimeout);
            IGuildChannel guildChannel = responseMessage.MentionedChannels.FirstOrDefault();
            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);
            return guildChannel;
        }

        /// <summary>
        /// Helper Function to simplify asking for a Role to mention
        /// </summary>
        /// <returns></returns>
        private async Task<IRole> _RequestNotificationRole()
        {
            //Context.Guild.
            var questionMessage = await ReplyAsync($"{Context.Message.Author.Mention}, What is the name of the Role you would like to mention in messages? Ex: {Context.Guild.CurrentUser.Roles.First(d => d.IsEveryone == false).Name}");
            var responseMessage = await NextMessageAsync(timeout: Defaults.MessageTimeout);
            IRole role = responseMessage.MentionedRoles.FirstOrDefault();
            if (role == null)
            {
                string response = responseMessage.Content.Trim();
                if (response.Equals("everyone", StringComparison.CurrentCultureIgnoreCase))
                {
                    role = Context.Guild.EveryoneRole;
                }
                else
                {
                    role = Context.Guild.Roles.FirstOrDefault(d => d.Name.Equals(response, StringComparison.CurrentCultureIgnoreCase));
                }
            }
            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);
            return role;
        }

        private async Task<string> _RequestNotificationMessage()
        {
            string question = $"{Context.Message.Author.Mention}, What message should I send on notification?\n";
            question += "Type \"Default\" for the message to be: {Defaults.NotificationMessage}\n";
            question += "\nParameters:\n";
            question += "{role} - Role to ping (if applicable)\n";
            question += "{name} - Streamers Name\n";
            question += "{game} - Game they are playing\n";
            question += "{url} - URL to the stream\n";
            question += "{title} - Stream Title\n";

            var questionMessage = await ReplyAsync(question);
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