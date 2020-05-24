using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using LiveBot.Core.Repository.Enums;
using LiveBot.Core.Repository.Interfaces.Stream;
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
        private List<ILiveBotMonitor> _monitors;

        public MonitorModule(List<ILiveBotMonitor> monitors)
        {
            _monitors = monitors;
        }

        private async Task _DeleteMessage(IMessage Message)
        {
            try
            {
                await Message.DeleteAsync();
            }
            catch
            { }
        }

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

        [Command("services")]
        [RequireOwner]
        [Summary("Check if a particular Stream Service is loaded by name")]
        public async Task MonitorServices(string serviceName)
        {
            ServiceEnum serviceEnum = (ServiceEnum)Enum.Parse(typeof(ServiceEnum), serviceName.ToUpper());
            ILiveBotMonitor monitor = _monitors.Where(m => m.ServiceType == serviceEnum).First();
            await ReplyAsync($"Loaded: {monitor.ServiceType.ToString()}");
        }

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

            await _DeleteMessage(Context.Message);
            await ReplyAndDeleteAsync($"{Context.Message.Author.Mention}, {permissionsResult}", timeout: TimeSpan.FromMinutes(30));
        }

        [Command("list")]
        public async Task MonitorList()
        {
            await ReplyAsync($"This command isn't implemented yet");
        }

        [Command("start")]
        [Alias("edit")]
        [Summary("Setup a new Stream to monitor for this Discord")]
        public async Task MonitorStart()
        {
            await ReplyAsync($"This command isn't implemented yet");
        }

        [Command("start")]
        [Alias("edit", "add")]
        [Summary("Setup a new Stream to monitor for this Discord")]
        public async Task MonitorStart(ILiveBotMonitor monitor)
        {
            await ReplyAsync($"This command isn't implemented yet, but you input {monitor.ServiceType}");
        }

        [Command("stop")]
        [Alias("end", "remove")]
        [Summary("Stop monitoring a Stream for this Discord")]
        public async Task MonitorStop()
        {
            await ReplyAsync($"This command isn't implemented yet");
        }

        [Command("stop")]
        [Alias("end")]
        [Summary("Stop monitoring a Stream for this Discord")]
        public async Task MonitorStop(ILiveBotMonitor monitor)
        {
            await ReplyAsync($"This command isn't implemented yet, but you input {monitor.ServiceType}");
        }
    }
}