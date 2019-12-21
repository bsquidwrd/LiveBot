using Discord.Commands;
using LiveBot.Core.Repository;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    public class ExampleRepoCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly IExampleRepository _exampleRepository;

        public ExampleRepoCommand(IExampleRepository exampleRepository)
        {
            _exampleRepository = exampleRepository;
        }

        [Command("RepoExample")]
        public async Task RepoExampleAsync()
        {
            _exampleRepository.RepoCall();
            await ReplyAsync("Re called the repo method! Check the console!");
        }
    }
}