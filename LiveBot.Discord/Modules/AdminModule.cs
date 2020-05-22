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
        public AdminModule()
        {
        }

        [RequireOwner]
        [Command("restart")]
        public async Task RestartBotAsync(List<ILiveBotMonitor> monitors)
        {
            await Context.Client.SetStatusAsync(UserStatus.Invisible);

            try
            {
                monitors?.ForEach(i => i._Stop());
            }
            catch
            { }

            var msg = $@"{Context.User.Mention}, I am restarting. Enjoy the silence, you monster!";
            Embed embed = new EmbedBuilder().WithImageUrl("https://i.imgur.com/XSi0zrl.png").Build();
            await ReplyAsync(message: msg, embed: embed);
            Log.Information($"Restart initiated by {Context.Message.Author.Username} in {Context.Guild.Name} ({Context.Guild.Id})");

            await Context.Client.StopAsync();
            Environment.Exit(0);
        }
    }
}