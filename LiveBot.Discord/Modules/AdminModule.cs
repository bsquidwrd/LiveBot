using Discord.Commands;
using LiveBot.Core.Repository;
using Serilog;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [DontAutoLoad]
    public class AdminModule : ModuleBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;

        public AdminModule(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        [RequireOwner]
        [Command("restart")]
        public async Task RestartBotAsync()
        {
            var msg = $@"{Context.User.Mention}, I am restarting. Enjoy the silence, you monster! https://tenor.com/view/shrek-gingerbread-monster-gif-4149488";
            await ReplyAsync(msg);
            Log.Information($"Restart initiated by {Context.Message.Author.Username} in {Context.Guild.Name} ({Context.Guild.Id})");
            Environment.Exit(0);
        }
    }
}