using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    public class PublicModule : InteractiveBase<ShardedCommandContext>
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
            await ReplyAndDeleteAsync($"{Context.Message.Author.Mention} You can find my source code here: https://www.github.com/bsquidwrd/Live-Bot", timeout: TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Provides a Discord Server invite to my Support Server
        /// </summary>
        /// <returns></returns>
        [Command("support")]
        public async Task SupportAsync()
        {
            await ReplyAndDeleteAsync($"{Context.Message.Author.Mention}, you can find my support server here: https://discord.gg/zXkb4JP", timeout: TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Provides a link on how to support me via PayPal
        /// </summary>
        /// <returns></returns>
        [Command("donate")]
        public async Task DonateAsync()
        {
            await ReplyAndDeleteAsync($"{Context.Message.Author.Mention}, Thank you so much for even considering donating! You can donate here: <https://www.paypal.me/bsquidwrd>", timeout: TimeSpan.FromMinutes(1));
        }
    }
}