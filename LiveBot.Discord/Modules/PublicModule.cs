using Discord;
using Discord.Commands;
using LiveBot.Core.Repository;
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
    }
}