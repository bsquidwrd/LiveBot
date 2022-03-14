using Discord.Interactions;
using Discord.Rest;

namespace LiveBot.Discord.SlashCommands.Modules
{
    [RequireOwner]
    [Group(name: "admin", description: "Administrative functions for the Bot Owner")]
    public class AdminModule : RestInteractionModuleBase<RestInteractionContext>
    {
        private readonly IServiceProvider _services;

        public AdminModule(IServiceProvider services)
        {
            _services = services;
        }

        [SlashCommand(name: "ping", description: "Ping the bot")]
        public async Task PingAsync()
        {
            await DeferAsync(ephemeral: true);
            var timeDifference = DateTimeOffset.UtcNow - Context.Interaction.CreatedAt.ToUniversalTime();
            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = $"Took {timeDifference:hh\\:mm\\:ss\\.fff} to respond";
            });
            //await FollowupAsync(text: $"Took {timeDifference:hh\\:mm\\:ss\\.fff} to respond", ephemeral: true);
        }
    }
}