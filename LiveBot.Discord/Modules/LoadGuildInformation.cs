using Discord.WebSocket;
using LiveBot.Core.Repository;
using Serilog;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    internal class LoadGuildInformation
    {
        private readonly IExampleRepository _exampleRepository;

        public LoadGuildInformation(IExampleRepository exampleRepository)
        {
            _exampleRepository = exampleRepository;
        }

        public Task DoGuildInfo(SocketGuild guild)
        {
            _exampleRepository.TestCall();
            Log.Information($@"Guild {guild.Name} has become available");
            return Task.CompletedTask;
        }
    }
}