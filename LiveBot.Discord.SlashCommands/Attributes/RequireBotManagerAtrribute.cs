using Discord;
using Discord.Interactions;

namespace LiveBot.Discord.SlashCommands.Attributes
{
    public class RequireBotManager : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            ulong managerRoleId = 946658451495456778;

            var appinfo = await context.Client.GetApplicationInfoAsync();
            var guilduser = await context.Guild.GetUserAsync(context.User.Id);
            if (
                appinfo.Owner.Id == context.User.Id ||
                context.Guild.OwnerId == context.User.Id ||
                guilduser.GuildPermissions.Administrator ||
                guilduser.GuildPermissions.ManageGuild ||
                guilduser.RoleIds.Contains(managerRoleId)
                )
            {
                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError("You are not allowed to run this command");
        }
    }
}