using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Discord.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.Administrator | GuildPermission.ManageGuild)]
    [Group("monitor")]
    [Summary("Monitor actions for Streams")]
    public class MonitorModule : InteractiveBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;
        private readonly List<ILiveBotMonitor> _monitors;
        private readonly FormatStreamMessage formatStreamMessage;

        /// <summary>
        /// Represents the list of Monitoring Commands available
        /// </summary>
        /// <param name="monitors">The loaded Monitoring Services, later used for locating and processing requests</param>
        /// <param name="factory">The database factory so that the database can be utilized</param>
        public MonitorModule(List<ILiveBotMonitor> monitors, IUnitOfWorkFactory factory)
        {
            _monitors = monitors;
            _work = factory.Create();
            formatStreamMessage = new FormatStreamMessage();
        }

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

        [Command("check")]
        [RequireOwner]
        [Summary("Check if Stream is live. Basic debug command")]
        public async Task CheckStream(ILiveBotUser user)
        {
            ILiveBotMonitor monitor = _GetServiceMonitor(user);
            try
            {
                ILiveBotStream stream = await monitor.GetStream(user);
                await ReplyAsync($"Service: `{stream.ServiceName}`\nDisplay Name: `{stream.User.DisplayName}`\nGame: `{stream.Game.Name}`\nTitle: `{stream.Title}`");
            }
            catch
            {
                await ReplyAsync($"{user.ServiceName} Doesn't look like the user {user.DisplayName} is live");
            }
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
        [Command("list")]
        public async Task MonitorList()
        {
            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((g => g.DiscordId == Context.Guild.Id));
            await ReplyAsync($"This command isn't implemented yet");
        }

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
            TimeSpan defaultTimeout = TimeSpan.FromMinutes(1);
            IMessage questionMessage;
            IMessage responseMessage;
            IRole mentionRole;
            string notificationMessage;
            ILiveBotMonitor monitor = _GetServiceMonitor(user);

            IGuildChannel notificationChannel = await _RequestNotificationChannel();
            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(g => g.DiscordId == Context.Guild.Id);
            DiscordChannel discordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(c => c.DiscordId == notificationChannel.Id);

            string messageParameters = "";
            messageParameters += "{name} - Streamers Name\n";
            messageParameters += "{game} - Game they are playing\n";
            messageParameters += "{url} - URL to the stream\n";
            messageParameters += "{title} - Stream Title\n";

            questionMessage = await ReplyAsync($"{Context.Message.Author.Mention}, What message should I send on notification?");
            responseMessage = await NextMessageAsync(timeout: defaultTimeout);
            notificationMessage = responseMessage.Content;
            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);

            questionMessage = await ReplyAsync($"{Context.Message.Author.Mention}, Should I mention a\n`1` Role\n`2` Everyone\n`3` Neither");
            responseMessage = await NextMessageAsync(timeout: defaultTimeout);

            ILiveBotStream stream = await monitor.GetStream(user);
            await ReplyAsync(formatStreamMessage.GetNotificationMessage(stream, notificationMessage));

            await ReplyAsync($"{Context.Message.Author.Mention} This command isn't implemented yet, but you input {user.DisplayName} in service {monitor.ServiceType}");
        }

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
            ILiveBotMonitor monitor = _GetServiceMonitor(user);
            await ReplyAsync($"{Context.Message.Author.Mention} This command isn't implemented yet, but you input {user.DisplayName} in service {monitor.ServiceType}");
        }

        // Helper Functions
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
            var response = await NextMessageAsync(timeout: TimeSpan.FromMinutes(1));

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
            var questionMessage = await ReplyAsync($"Please mention the Discord Channel you would like to start or stop a notification for. Ex: {MentionUtils.MentionChannel(Context.Channel.Id)}");
            var responseMessage = await NextMessageAsync(timeout: TimeSpan.FromMinutes(1));
            IGuildChannel guildChannel = responseMessage.MentionedChannels.FirstOrDefault();
            await _DeleteMessage(questionMessage);
            await _DeleteMessage(responseMessage);
            return guildChannel;
        }
    }
}