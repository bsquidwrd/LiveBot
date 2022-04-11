﻿using Discord;
using Discord.Interactions;
using LiveBot.Core.Repository.Interfaces;

namespace LiveBot.Discord.SlashCommands.Attributes
{
    public class RequireBotManager : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            var appinfo = await context.Client.GetApplicationInfoAsync();
            var guilduser = await context.Guild.GetUserAsync(context.User.Id);
            if (
                appinfo.Owner.Id == context.User.Id ||
                context.Guild.OwnerId == context.User.Id ||
                guilduser.GuildPermissions.Administrator ||
                guilduser.GuildPermissions.ManageGuild
                )
            {
                return PreconditionResult.FromSuccess();
            }

            var _work = services.GetRequiredService<IUnitOfWorkFactory>().Create();
            var guildConfig = await _work.GuildConfigRepository.SingleOrDefaultAsync(x => x.DiscordGuild.DiscordId == context.Guild.Id);
            ulong? adminRoleId = guildConfig.AdminRoleDiscordId;
            if (adminRoleId != null)
            {
                ulong? roleId = guilduser.RoleIds.Where(i => i == adminRoleId).FirstOrDefault();
                if (roleId == adminRoleId)
                    return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError("You are not allowed to run this command");
        }
    }
}