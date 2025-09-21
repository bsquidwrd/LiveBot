using Discord;
using Discord.Interactions;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Core.Repository.Static;

namespace LiveBot.Discord.SlashCommands.Modules
{
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public class ConfigModule : InteractionModuleBase<ShardedInteractionContext>
    {
        private readonly ILogger<ConfigModule> _logger;
        private readonly IUnitOfWork _work;

        public ConfigModule(ILogger<ConfigModule> logger, IUnitOfWorkFactory factory)
        {
            _logger = logger;
            _work = factory.Create();
        }

        #region Config command

        [SlashCommand(name: "config", description: "Configure some general settings for the server overall")]
        public async Task ConfigSetupAsync(
            [Summary(name: "default-live-message", description: "This message will be sent out when the streamer goes live (check /monitor help)")]
            string? LiveMessage = null
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

            var ResponseMessage = "";

            if (LiveMessage != null)
            {
                if (LiveMessage.Equals("default", StringComparison.InvariantCultureIgnoreCase))
                {
                    LiveMessage = Defaults.NotificationMessage;
                }
                guildConfig.Message = LiveMessage;
                ResponseMessage += $"Updated guild default live message to be {Format.Code(LiveMessage)}. ";
            }

            await _work.GuildConfigRepository.UpdateAsync(guildConfig);

            if (!string.IsNullOrWhiteSpace(ResponseMessage))
                ResponseMessage = $"Updated Guild Config information. {ResponseMessage}";
            else
                ResponseMessage = "Nothing was updated";

            var configEmbed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .WithFooter(text: Context.Guild.Id.ToString())
                .AddField(name: "Bot Manager Role", value: (guildConfig?.AdminRoleDiscordId == null ? "none" : MentionUtils.MentionRole((ulong)guildConfig.AdminRoleDiscordId)), inline: false)
                .AddField(name: "Default Live Message", value: Format.Code((guildConfig?.Message ?? Defaults.NotificationMessage)), inline: false)
                .AddField(name: "Role Config - Monitor", value: (guildConfig?.MonitorRoleDiscordId == null ? "none" : MentionUtils.MentionRole((ulong)guildConfig.MonitorRoleDiscordId)), inline: true)
                .AddField(name: "Role Config - Mention", value: (guildConfig?.MentionRoleDiscordId == null ? "none" : MentionUtils.MentionRole((ulong)guildConfig.MentionRoleDiscordId)), inline: true)
                .AddField(name: "Role Config - Channel", value: (guildConfig?.DiscordChannel?.DiscordId == null ? "none" : MentionUtils.MentionChannel((ulong)guildConfig.DiscordChannel.DiscordId)), inline: true)
                .Build();

            await FollowupAsync(text: ResponseMessage, embed: configEmbed, ephemeral: true);
        }

        #endregion Config command
    }
}