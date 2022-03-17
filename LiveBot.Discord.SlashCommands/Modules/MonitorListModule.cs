using Discord.Interactions;
using Discord.Rest;

namespace LiveBot.Discord.SlashCommands.Modules
{
    public partial class MonitorModule : RestInteractionModuleBase<RestInteractionContext>
    {
        /// <summary>
        /// List all stream monitors in the Guild
        /// </summary>
        /// <returns></returns>
        [SlashCommand(name: "list", description: "List all stream monitors", runMode: RunMode.Async)]
        public async Task ListStreamMonitor()
        {
            try
            {
                await DeferAsync(ephemeral: true);
            }
            catch { }

            var subscriptions = await _work.SubscriptionRepository.FindAsync(i => i.DiscordGuild.DiscordId == Context.Guild.Id);

            await FollowupAsync($"Ahhh... got {subscriptions.LongCount()} subscriptions", ephemeral: true);
        }
    }
}