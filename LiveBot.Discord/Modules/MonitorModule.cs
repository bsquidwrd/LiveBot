using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Stream;
using LiveBot.Core.Repository.Models.Discord;
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

        /// <summary>
        /// Simple method to delete a message and not fail
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
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
        /// Gives a list of loaded Monitoring Services
        /// </summary>
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
        [Command("test")]
        [Alias("check", "perms")]
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
        /// <returns></returns>
        [Command("list")]
        public async Task MonitorList()
        {
            DiscordGuild discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((g => g.DiscordId == Context.Guild.Id));
            await ReplyAsync($"This command isn't implemented yet");
        }

        /// <summary>
        /// Runs through the process of Starting a Stream from being monitored
        /// </summary>
        /// <returns></returns>
        [Command("start")]
        [Alias("edit")]
        [Summary("Setup a new Stream to monitor for this Discord")]
        public async Task MonitorStart()
        {
            await ReplyAsync($"This command isn't implemented yet");
        }

        /// <summary>
        /// Runs through the process of Starting a Stream from being monitored
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [Command("start")]
        [Alias("edit", "add")]
        [Summary("Setup a new Stream to monitor for this Discord")]
        public async Task MonitorStart(ILiveBotUser user)
        {
            ILiveBotMonitor monitor = _GetServiceMonitor(user);
            await ReplyAsync($"This command isn't implemented yet, but you input {user.DisplayName} in service {monitor.ServiceType}");
        }

        /// <summary>
        /// Runs through the process of Stopping a Stream from being monitored
        /// </summary>
        /// <returns></returns>
        [Command("stop")]
        [Alias("end", "remove")]
        [Summary("Stop monitoring a Stream for this Discord")]
        public async Task MonitorStop()
        {
            await ReplyAsync($"This command isn't implemented yet");
        }


        /// <summary>
        /// Runs through the process of Stopping a Stream from being monitored
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [Command("stop")]
        [Alias("end")]
        [Summary("Stop monitoring a Stream for this Discord")]
        public async Task MonitorStop(ILiveBotUser user)
        {
            ILiveBotMonitor monitor = _GetServiceMonitor(user);
            await ReplyAsync($"This command isn't implemented yet, but you input {user.DisplayName} in service {monitor.ServiceType}");
        }

        // Helper Functions
        /// <summary>
        /// Attempts to locate the loaded Monitoring Service for the given <c>ILiveBotBase</c>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>ILiveBotMonitor object which represents a loaded Monitoring Service</returns>
        private ILiveBotMonitor _GetServiceMonitor(ILiveBotBase obj)
        {
            return _monitors.Where(o => o.ServiceType == obj.ServiceType).First();
        }
    }
}