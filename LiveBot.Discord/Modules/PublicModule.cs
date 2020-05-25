using Discord;
using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    public class PublicModule : ModuleBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;

        public PublicModule(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        /// <summary>
        /// Gets general information about the bot
        /// </summary>
        /// <returns></returns>
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
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

        /// <summary>
        /// General command to test that the bot can send messages, as well as a cheeky way of getting my name out there
        /// </summary>
        /// <returns></returns>
        [Command("hello")]
        public async Task HelloAsync()
        {
            var AppInfo = await Context.Client.GetApplicationInfoAsync();
            var msg = $"Hello, I am a bot created by {AppInfo.Owner.Username}#{AppInfo.Owner.DiscriminatorValue}";
            await ReplyAsync(msg);
        }

        /// <summary>
        /// Provides a link to the GitHub Repository for the bot
        /// </summary>
        /// <returns></returns>
        [Command("source")]
        public async Task SourceAsync()
        {
            var AppInfo = await Context.Client.GetApplicationInfoAsync();
            var msg = $"{Context.Message.Author.Mention} You can find my source code here: https://www.github.com/bsquidwrd/Live-Bot";
            await ReplyAsync(msg);
        }
    }
}