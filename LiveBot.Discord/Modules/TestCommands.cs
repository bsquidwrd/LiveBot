using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Discord.Services.LiveBot;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [RequireOwner]
    [Group("test")]
    public class TestCommands : ModuleBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;

        public TestCommands(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        [Command("retrieve")]
        public async Task RetrieveAsync()
        {
            DiscordGuild DBGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == Context.Guild.Id));
            DiscordChannel DBChannel = await _work.ChannelRepository.SingleOrDefaultAsync((c => c.DiscordGuild == DBGuild && c.DiscordId == Context.Channel.Id));
            await ReplyAsync($"The following names were retrieved from the Database: Channel {DBChannel.Name} in Guild {DBGuild.Name}");
        }

        [Command("url")]
        public async Task URLAsync(BaseStreamChannel streamChannel)
        {
            await ReplyAsync($"Result: {streamChannel.GetUsername()}");
        }

        [Command("bool")]
        public async Task BoolAsync(bool input)
        {
            await ReplyAsync($"Result: {input}");
        }

        [Command("api")]
        public async Task APIAsync(string bearerToken)
        {
            await ReplyAsync($"Result: Pong");
        }
    }
}