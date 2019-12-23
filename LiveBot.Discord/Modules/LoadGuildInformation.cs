using Discord.WebSocket;
using LiveBot.Core.Repository;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    internal class LoadGuildInformation
    {
        private readonly IUnitOfWork _work;

        public LoadGuildInformation(IUnitOfWorkFactory factory)
        {
            _work = factory.Create();
        }

        public Task DoGuildInfo(SocketGuild guild)
        {
            _work.GuildRepository.UpdateOrCreateGuild(guild.Id, guild.Name);
            return Task.CompletedTask;
        }
    }
}