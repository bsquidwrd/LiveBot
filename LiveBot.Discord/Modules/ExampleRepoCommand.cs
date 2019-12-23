using Discord.Commands;
using LiveBot.Core.Repository;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    public class ExampleRepoCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly IUnitOfWork _work;

        public ExampleRepoCommand(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        [Command("RepoExample")]
        public async Task RepoExampleAsync()
        {
            _work.ExampleRepository.RepoCall();
            await ReplyAsync("Re called the repo method! Check the console!");
        }
    }
}