using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using LiveBot.Core.Repository.Interfaces.Stream;
using LiveBot.Discord.Services.LiveBot;
using Serilog;
using System;
using System.Collections.Generic;
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
        public async Task MonitorServices()
        {
            foreach (ILiveBotMonitor monitor in _monitors)
            {
                await ReplyAsync($"{monitor.ServiceType.ToString()}");
            }
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
        public async Task MonitorStart(BaseStreamChannel streamChannel)
        {
            await ReplyAsync($"This command isn't implemented yet, but you input {streamChannel.GetUsername()}");
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
        public async Task MonitorStop(BaseStreamChannel streamChannel)
        {
            await ReplyAsync($"This command isn't implemented yet, but you input {streamChannel.GetUsername()}");
        }
    }
}