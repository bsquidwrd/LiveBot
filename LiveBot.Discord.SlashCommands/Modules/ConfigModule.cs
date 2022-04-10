using Discord;
using Discord.Interactions;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Models.Discord;
using LiveBot.Discord.SlashCommands.Attributes;

namespace LiveBot.Discord.SlashCommands.Modules
{
    [RequireBotManager]
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
            [Summary(name: "admin-role", description: "The role allowed to manage the bot, so you don't need to assign Manage Server")] IRole? AdminRole = null
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
            if (AdminRole != null)
            {
                if (AdminRole.Id != Context.Guild.EveryoneRole.Id)
                {
                    guildConfig.AdminRoleDiscordId = AdminRole.Id;
                    ResponseMessage += $"Set {Format.Bold(AdminRole?.Name)} as the bot manager role. ";
                }
                else
                {
                    ResponseMessage += "You can't set the bot manager role to everyone. ";
                }
            }

            await _work.GuildConfigRepository.UpdateAsync(guildConfig);

            if (!string.IsNullOrWhiteSpace(ResponseMessage))
                ResponseMessage = $"Updated Guild Config information. {ResponseMessage}";
            else
                ResponseMessage = "Nothing was updated";

            await FollowupAsync(text: ResponseMessage, ephemeral: true);
        }

        #endregion Config command
    }
}