using Discord;
using Discord.Interactions;

namespace LiveBot.Discord.SlashCommands.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequireBotOwnerAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            if (context.Client.TokenType == TokenType.Bot)
            {
                IApplication application = await context.Client.GetApplicationInfoAsync().ConfigureAwait(continueOnCapturedContext: false);
                ulong ownerId = application.Team == null ? application.Owner.Id : application.Team.OwnerUserId;
                if (context.User.Id != ownerId)
                {
                    return PreconditionResult.FromError(ErrorMessage ?? "Command can only be run by the owner of the bot.");
                }

                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError("RequireOwnerAttribute is not supported by this TokenType.");
        }
    }
}