using Discord.Commands;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [Group("test")]
    public class TestCommands : ModuleBase<ShardedCommandContext>
    {
        [Command("ping")]
        public async Task TestAsync()
        {
            var msg = $@"Hi, I am a bot: {Context.Client.CurrentUser.IsBot}";
            await ReplyAsync(msg);
        }
    }
}