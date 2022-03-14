using Discord.Interactions;
using Discord.Rest;

namespace LiveBot.Discord.SlashCommands.Modules
{
    public class GeneralModule : RestInteractionModuleBase<RestInteractionContext>
    {
        [SlashCommand(name: "ping", description: "Ping the bot")]
        public async Task PingAsync()
        {
            await DeferAsync();
            await FollowupAsync("Pong!");
        }
    }
}