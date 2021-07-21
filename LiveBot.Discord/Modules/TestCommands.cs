using Discord.Commands;
using Interactivity;
using Interactivity.Pagination;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Static;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [RequireOwner]
    [Group("test")]
    public class TestCommands : ModuleBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Testing Commands for the bot
        /// </summary>
        /// <param name="factory"></param>
        public TestCommands(IUnitOfWorkFactory factory, InteractivityService interactivity)
        {
            _work = factory.Create();
            _interactivity = interactivity;
        }

        [Command("bool")]
        public async Task TestBoolAsync(bool value)
        {
            await ReplyAsync($"I evaluated {value}");
        }

        /// <summary>
        /// Used to check that the Bot is communincating with the Database properly
        /// </summary>
        /// <returns></returns>
        [Command("retrieve")]
        public async Task RetrieveAsync()
        {
            DiscordGuild DBGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == Context.Guild.Id));
            DiscordChannel DBChannel = await _work.ChannelRepository.SingleOrDefaultAsync((c => c.DiscordGuild == DBGuild && c.DiscordId == Context.Channel.Id));
            await ReplyAsync($"The following names were retrieved from the Database: Channel {DBChannel.Name} in Guild {DBGuild.Name}");
        }

        ///<summary>
        /// NextMessageAsync will wait for the next message to come in over the gateway, given certain criteria
        /// By default, this will be limited to messages from the source user in the source channel
        /// This method will block the gateway, so it should be ran in async mode.
        /// </summary>
        [Command("next", RunMode = RunMode.Async)]
        public async Task Test_NextMessageAsync()
        {
            await ReplyAsync("What is 2+2?");
            var response = await _interactivity.NextMessageAsync(x => x.Author.Id == Context.User.Id, timeout: Defaults.MessageTimeout);
            if (response != null)
                await ReplyAsync($"You replied: {response.Value.Content}");
            else
                await ReplyAsync("You did not reply before the timeout");
        }

        [Command("paginator", RunMode = RunMode.Async)]
        public Task PaginatorAsync()
        {
            var pages = new PageBuilder[] {
                new PageBuilder().WithTitle("I"),
                new PageBuilder().WithTitle("am"),
                new PageBuilder().WithTitle("cool"),
                new PageBuilder().WithTitle(":sunglasses:"),
                new PageBuilder().WithText("I am cool :crown:")
            };

            var paginator = new StaticPaginatorBuilder()
                .WithUsers(Context.User)
                .WithPages(pages)
                .WithFooter(PaginatorFooter.PageNumber | PaginatorFooter.Users)
                .WithDefaultEmotes()
                .Build();

            return _interactivity.SendPaginatorAsync(paginator, Context.Channel, TimeSpan.FromMinutes(2));
        }
    }
}