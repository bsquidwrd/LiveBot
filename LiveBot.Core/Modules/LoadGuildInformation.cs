using System.Threading.Tasks;
using Discord.WebSocket;
using Serilog;

namespace LiveBot.Core.Modules
{
    class LoadGuildInformation
    {

        public Task DoGuildInfo(SocketGuild guild)
        {
            Log.Information($@"Guild {guild.Name} has become available");
            return Task.CompletedTask;
        }
    }
}
