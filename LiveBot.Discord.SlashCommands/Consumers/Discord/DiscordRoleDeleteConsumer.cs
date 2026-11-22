using LiveBot.Core.Contracts.Discord;
using LiveBot.Core.Repository.Interfaces;
using MassTransit;

namespace LiveBot.Discord.SlashCommands.Consumers.Discord
{
    public class DiscordRoleDeleteConsumer : IConsumer<IDiscordRoleDelete>
    {
        private readonly IUnitOfWork _work;
        private readonly ILogger<DiscordRoleDeleteConsumer> _logger;

        public DiscordRoleDeleteConsumer(IUnitOfWork work, ILogger<DiscordRoleDeleteConsumer> logger)
        {
            _work = work;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IDiscordRoleDelete> context)
        {
            var message = context.Message;

            var rolesToMention = await _work.RoleToMentionRepository.FindAsync(i => i.DiscordRoleId == message.RoleId);
            await _work.RoleToMentionRepository.RemoveRangeAsync(rolesToMention.Select(i => i.Id).ToList());

            var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == message.GuildId);
            if (guildConfig != null)
            {
                bool update = false;
                if (guildConfig.MonitorRoleDiscordId == message.RoleId)
                {
                    guildConfig.MonitorRoleDiscordId = null;
                    update = true;
                }
                if (guildConfig.MentionRoleDiscordId == message.RoleId)
                {
                    guildConfig.MentionRoleDiscordId = null;
                    update = true;
                }
                if (guildConfig.AdminRoleDiscordId == message.RoleId)
                {
                    guildConfig.AdminRoleDiscordId = null;
                    update = true;
                }

                if (update)
                    await _work.GuildConfigRepository.UpdateAsync(guildConfig);
            }
        }
    }
}