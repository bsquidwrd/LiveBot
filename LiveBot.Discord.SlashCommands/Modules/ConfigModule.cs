using Discord;
using Discord.Interactions;
using Discord.Rest;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Static;
using LiveBot.Discord.SlashCommands.Attributes;

namespace LiveBot.Discord.SlashCommands.Modules
{
    [RequireBotManager]
    public class ConfigModule : RestInteractionModuleBase<RestInteractionContext>
    {
        private readonly ILogger<ConfigModule> _logger;
        private readonly IUnitOfWork _work;

        public ConfigModule(ILogger<ConfigModule> logger, IUnitOfWorkFactory factory)
        {
            _logger = logger;
            _work = factory.Create();
        }

        [SlashCommand(name: "config", description: "Configure some general settings for the server")]
        public async Task ConfigSetupAsync(
            [Summary(name: "where-to-post", description: "The channel to post live alerts to")] ITextChannel? WhereToPost = null,
            [Summary(name: "live-message", description: "This message will be sent out when the streamer goes live (check help for more info)")] string LiveMessage = Defaults.NotificationMessage,
            [Summary(name: "role-to-mention", description: "The role to replace {role} with in the live message (default is none)")] IRole? RoleToMention = null,
            [Summary(name: "role-to-monitor", description: "The role to replace {role} with in the live message (default is none)")] IRole? RoleToMonitor = null,
            [Summary(name: "admin-role", description: "The role to replace {role} with in the live message (default is none)")] IRole? AdminRole = null,
            [Summary(name: "stop-monitoring", description: "Stop monitoring a role")] bool StopMonitoring = false
        )
        {
            var discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == Context.Guild.Id);
            if (discordGuild == null)
            {
                var newDiscordGuild = new DiscordGuild
                {
                    DiscordId = Context.Guild.Id,
                    Name = Context.Guild.Name,
                    IconUrl = Context.Guild.IconId
                };
                await _work.GuildRepository.AddOrUpdateAsync(newDiscordGuild, i => i.DiscordId == Context.Guild.Id);
            }
            discordGuild = await _work.GuildRepository.SingleOrDefaultAsync(i => i.DiscordId == Context.Guild.Id);

            var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id);
            if (guildConfig == null)
            {
                var newGuildConfig = new DiscordGuildConfig
                {
                    DiscordGuild = discordGuild
                };
                await _work.GuildConfigRepository.AddOrUpdateAsync(newGuildConfig, i => i.DiscordGuild.DiscordId == Context.Guild.Id);
            }
            guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id);

            if (WhereToPost != null)
                guildConfig.DiscordChannel = await _work.ChannelRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id && i.DiscordId == WhereToPost.Id);

            if (LiveMessage != null)
            {
                if (LiveMessage.Equals("default", StringComparison.InvariantCultureIgnoreCase))
                {
                    LiveMessage = Defaults.NotificationMessage;
                }
                guildConfig.Message = LiveMessage;
            }

            if (RoleToMonitor != null)
                guildConfig.MonitorRole = await _work.RoleRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id && i.DiscordId == RoleToMonitor.Id);

            if (RoleToMention != null)
                guildConfig.DiscordRole = await _work.RoleRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id && i.DiscordId == RoleToMention.Id);

            if (AdminRole != null)
                guildConfig.AdminRole = await _work.RoleRepository.SingleOrDefaultAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id && i.DiscordId == AdminRole.Id);

            if (AdminRole?.Id == Context.Guild.EveryoneRole.Id)
                guildConfig.AdminRole = null;

            if (StopMonitoring)
                guildConfig.MonitorRole = null;

            await _work.GuildConfigRepository.UpdateAsync(guildConfig);

            await FollowupAsync($"Updated config information for {Format.Bold(Context.Guild.Name)}", ephemeral: true);
        }
    }
}