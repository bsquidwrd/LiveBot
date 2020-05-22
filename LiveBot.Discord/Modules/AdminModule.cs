using Discord;
using Discord.Commands;
using LiveBot.Core.Repository.Interfaces.Stream;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    //[DontAutoLoad]
    [RequireOwner]
    [Group("admin")]
    public class AdminModule : ModuleBase<ShardedCommandContext>
    {
        //private readonly List<ILiveBotMonitor> Monitors;

        public AdminModule()
        //public AdminModule(List<ILiveBotMonitor> monitors)
        {
            //Monitors = monitors;
        }

        [RequireOwner]
        [Command("restart")]
        public async Task RestartBotAsync()
        {
            await Context.Client.SetStatusAsync(UserStatus.Invisible);

            //try
            //{
            //    foreach (ILiveBotMonitor Monitor in Monitors)
            //    {
            //        await Monitor._Stop();
            //    }
            //}
            //catch
            //{ }

            var msg = $@"{Context.User.Mention}, I am restarting. Enjoy the silence, you monster! https://tenor.com/view/shrek-gingerbread-monster-gif-4149488";
            await ReplyAsync(msg);
            Log.Information($"Restart initiated by {Context.Message.Author.Username} in {Context.Guild.Name} ({Context.Guild.Id})");

            await Context.Client.StopAsync();
            Environment.Exit(0);
        }
    }
}