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
            discordGuild = await _work.GuildRepository.SingleOrDefaultAsync((d => d.DiscordId == guild.Id));

            foreach (SocketGuildChannel channel in guild.Channels)
            {
                DiscordChannel discordChannel = new DiscordChannel() { DiscordGuild = discordGuild, DiscordId = channel.Id, Name = channel.Name };
                await _work.ChannelRepository.AddOrUpdateAsync(discordChannel, (c => c.DiscordGuild == discordGuild && c.DiscordId == channel.Id));
            }
        }
    }
}