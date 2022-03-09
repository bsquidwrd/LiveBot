using Discord.Interactions;
using Discord.Rest;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Repository;
using Microsoft.EntityFrameworkCore;

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
            var timeDifference = DateTimeOffset.UtcNow - Context.Interaction.CreatedAt.ToUniversalTime();
            await RespondAsync(text: $"Took {timeDifference:hh\\:mm\\:ss\\.fff} to respond", ephemeral: true);
        }

        [SlashCommand(name: "test", description: "Random test command")]
        public async Task TestAsync(string input)
        {
            var factory = _services.GetRequiredService<IUnitOfWorkFactory>();
            var work = (UnitOfWork)factory.Create();
            var context = work.GetContext();
            var user = await context.StreamUser.Where(x => x.Deleted == false && x.Username == input).FirstOrDefaultAsync();

            var message = "";
            if (user == null)
            {
                message = $"Could not find a user with the username of {input}";
            }
            else
            {
                message = $"User: {user.DisplayName}";
                var subscriptions = String.Join(", ", user.StreamSubscriptions.ToList().Select(x => x.DiscordChannel.Name));
                message += $"\n{subscriptions}";
            }

            await RespondAsync(text: message, ephemeral: true);
        }
    }
}