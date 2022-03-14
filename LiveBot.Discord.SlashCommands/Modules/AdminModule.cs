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

        [SlashCommand(name: "ping", description: "Ping the bot", runMode: RunMode.Async)]
        public async Task PingAsync()
        {
            await DeferAsync(ephemeral: true);
            var timeDifference = DateTimeOffset.UtcNow - Context.Interaction.CreatedAt.ToUniversalTime();

            var timeToWait = TimeSpan.FromSeconds(5);
            _logger.LogInformation($"Waiting {timeToWait.TotalSeconds} seconds before continuing with the Admin Ping command");
            await Task.Delay(timeToWait);

            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = $"Took {timeDifference:hh\\:mm\\:ss\\.fff} to respond";
            });
            //await FollowupAsync(text: $"Took {timeDifference:hh\\:mm\\:ss\\.fff} to respond", ephemeral: true);
        }
    }
}