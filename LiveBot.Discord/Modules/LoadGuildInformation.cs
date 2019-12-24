using Discord.WebSocket;
using LiveBot.Core.Repository;
using LiveBot.Core.Repository.Models;
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

        public async Task DoGuildInfo(SocketGuild guild)
        {
            DiscordGuild discordGuild = new DiscordGuild() { DiscordId = guild.Id, Name = guild.Name };
            await _work.GuildRepository.AddOrUpdateAsync(discordGuild, (d => d.DiscordId == guild.Id));
        }
    }
}