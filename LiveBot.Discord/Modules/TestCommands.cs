using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.SiteAPIs;
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
        private readonly ISiteAPIs _siteAPIs;

        public TestCommands(IUnitOfWorkFactory factory, ISiteAPIsFactory siteAPIs)
        {
            _work = factory.Create();
            _siteAPIs = siteAPIs.Create();
        }

        [Command("ping")]
        public async Task TestAsync()
        {
            var msg = $@"Hi, I am a bot: {Context.Client.CurrentUser.IsBot}";
            await ReplyAsync(msg);
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
        public async Task APIAsync()
        {
            await ReplyAsync($"Result: {_siteAPIs.TwitchAPI.Value}");
        }
    }
}