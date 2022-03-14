using Discord.Interactions;
using Discord.Rest;

namespace LiveBot.Discord.SlashCommands.Modules
{
    [RequireOwner]
    [Group(name: "admin", description: "Administrative functions for the Bot Owner")]
    public class AdminModule : RestInteractionModuleBase<RestInteractionContext>
    {
        private readonly ILogger<AdminModule> _logger;

        public AdminModule(ILogger<AdminModule> logger)
        {
            _logger = logger;
        }

        [SlashCommand(name: "ping", description: "Ping the bot")]
        public async Task PingAsync()
        {
            try
            {
                await DeferAsync(ephemeral: true);
                var timeDifference = DateTimeOffset.UtcNow - Context.Interaction.CreatedAt.ToUniversalTime();
                await FollowupAsync(text: $"Took {timeDifference:hh\\:mm\\:ss\\.fff} to respond", ephemeral: true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to run admin ping command: {ex}");
            }
        }
    }
}