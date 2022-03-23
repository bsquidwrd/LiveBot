using Discord.Interactions;
using Discord.Rest;

namespace LiveBot.Discord.SlashCommands.Modules
{
    [RequireOwner]
    [DontAutoRegister]
    [Group(name: "admin", description: "Administrative functions for the Bot Owner")]
    public class AdminModule : RestInteractionModuleBase<RestInteractionContext>
    {
        private readonly ILogger<AdminModule> logger;
        private readonly IConfiguration configuration;
        private readonly InteractionService commands;

        public AdminModule(ILogger<AdminModule> logger, IConfiguration configuration, InteractionService commands)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.commands = commands;
        }

        [SlashCommand(name: "ping", description: "Ping the bot")]
        public async Task PingAsync()
        {
            var timeDifference = DateTimeOffset.UtcNow - Context.Interaction.CreatedAt.ToUniversalTime();
            await FollowupAsync(text: $"Took {timeDifference:hh\\:mm\\:ss\\.fff} to respond", ephemeral: true);
        }

        [SlashCommand(name: "divide", description: "Divide two numbers")]
        public async Task DivideAsync(int number1, int number2)
        {
            await FollowupAsync(text: $"Result: {number1 / number2}", ephemeral: true);
        }

        [SlashCommand(name: "register_commands", description: "Force a re-registration of the bot commands")]
        public async Task RegisterCommandsAsync()
        {
            var IsDebug = configuration.GetValue<bool>("IsDebug", false);
            var testGuildId = configuration.GetValue<ulong>("testguild");

            if (IsDebug)
                await commands.RegisterCommandsToGuildAsync(guildId: testGuildId, deleteMissing: true);
            else
                await commands.RegisterCommandsGloballyAsync(deleteMissing: true);

            var adminGuild = await commands.RestClient.GetGuildAsync(testGuildId);
            await commands.AddModulesToGuildAsync(guild: adminGuild, deleteMissing: false, modules: commands.GetModuleInfo<AdminModule>());
        }
    }
}