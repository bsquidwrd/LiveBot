using Discord.WebSocket;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    internal class LoadGuildInformation
    {
        public Task DoGuildInfo(SocketGuild guild)
        {
            //Log.Information($@"Guild {guild.Name} has become available");
            return Task.CompletedTask;
        }
    }
}