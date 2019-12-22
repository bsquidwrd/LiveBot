using Discord.Commands;
using LiveBot.Core.Repository;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [Group("test")]
    public class TestCommands : ModuleBase<ShardedCommandContext>
    {
        private readonly IGuildRepository _guildRepository;

        public TestCommands(IGuildRepository guildRepository)
        {
            _guildRepository = guildRepository;
        }

        [Command("ping")]
        public async Task TestAsync()
        {
            var msg = $@"Hi, I am a bot: {Context.Client.CurrentUser.IsBot}";
            await ReplyAsync(msg);
        }

        [Command("getguild")]
        public async Task GetGuildAsync()
        {
            var GuildID = _guildRepository.GetGuild(Context.Guild.Id);
            await ReplyAsync($"The command made it around the world! Your Guild ID is {GuildID}");
        }
    }
}