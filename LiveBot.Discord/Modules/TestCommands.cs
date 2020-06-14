using Discord.Addons.Interactive;
using Discord.Commands;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    [DontAutoLoad]
    [RequireOwner]
    [Group("test")]
    public class TestCommands : InteractiveBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;

        /// <summary>
        /// Testing Commands for the bot
        /// </summary>
        /// <param name="factory"></param>
        public TestCommands(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
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

        /// <summary>
        /// Test deleting a message after a set amount of time
        /// </summary>
        /// <returns></returns>
        [Command("delete")]
        public async Task<RuntimeResult> Test_DeleteAfterAsync()
        {
            await ReplyAndDeleteAsync("this message will delete in 10 seconds", timeout: TimeSpan.FromSeconds(10));
            return Ok();
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
            var response = await NextMessageAsync();
            if (response != null)
                await ReplyAsync($"You replied: {response.Content}");
            else
                await ReplyAsync("You did not reply before the timeout");
        }

        /// <summary>
        /// PagedReplyAsync will send a paginated message to the channel
        /// You can customize the paginator by creating a PaginatedMessage object
        /// You can customize the criteria for the paginator as well, which defaults to restricting to the source user
        /// This method will not block.
        /// </summary>
        [Command("paginator")]
        public async Task Test_Paginator()
        {
            var pages = new[] { "Page 1", "Page 2", "Page 3", "aaaaaa", "Page 5" };
            await PagedReplyAsync(pages);
        }
    }
}