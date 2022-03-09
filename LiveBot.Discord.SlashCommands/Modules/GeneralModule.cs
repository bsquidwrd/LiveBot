using Discord.Interactions;
using Discord.Rest;

namespace LiveBot.Discord.SlashCommands.Modules
{
    public class GeneralModule : RestInteractionModuleBase<RestInteractionContext>
    {
        [SlashCommand(name: "ping", description: "Ping the bot")]
        public Task PingAsync() =>
            RespondAsync("Pong!");
    }
}