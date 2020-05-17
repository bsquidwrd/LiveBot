using Discord;
using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    // Remember to make your module reference the ShardedCommandContext
    public class PublicModule : ModuleBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;

        public PublicModule(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        [Command("info")]
        public async Task InfoAsync()
        {
            var AppInfo = await Context.Client.GetApplicationInfoAsync();
            var ReplyEmbed = new EmbedBuilder()
                .WithTitle($"{AppInfo.Name} Information")
                //.WithDescription($"")
                .WithUrl("https://livebot.bsquid.io")
                .WithColor(Color.DarkPurple)
                .WithAuthor(AppInfo.Owner)
                .WithFooter(footer => footer.Text = $"Shard {Context.Client.GetShardFor(Context.Guild).ShardId + 1} / {Context.Client.Shards.Count}")
                .WithCurrentTimestamp()
                .Build();
            await ReplyAsync(null, false, ReplyEmbed);
        }

        [Command("hello")]
        public async Task HelloAsync()
        {
            var AppInfo = await Context.Client.GetApplicationInfoAsync();
            var msg = $@"Hello, I am a bot created by {AppInfo.Owner.Username}#{AppInfo.Owner.DiscriminatorValue}";
            await ReplyAsync(msg);
        }

        [Command("source")]
        public async Task SourceAsync()
        {
            var AppInfo = await Context.Client.GetApplicationInfoAsync();
            var msg = $@"{Context.Message.Author.Mention} You can find my source code here: https://www.github.com/bsquidwrd/Live-Bot";
            await ReplyAsync(msg);
        }
    }
}