using Discord;
using Discord.Interactions;
using Discord.Rest;

namespace LiveBot.Discord.SlashCommands.Modules
{
    public class HelpModule : RestInteractionModuleBase<RestInteractionContext>
    {
        private readonly ILogger<HelpModule> _logger;
        private readonly IServiceProvider _services;
        private readonly InteractionService _service;

        public HelpModule(ILogger<HelpModule> logger, IServiceProvider services, InteractionService service)
        {
            _logger = logger;
            _services = services;
            _service = service;
        }

        [SlashCommand(name: "help", description: "Get help")]
        public async Task HelpAsync()
        {
            await DeferAsync();
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use"
            };

            foreach (var module in _service.Modules)
            {
                if (module.SlashGroupName == null)
                    continue;
                if (module.GetType() == this.GetType())
                    continue;

                string? description = null;
                foreach (var cmd in module.SlashCommands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context, _services);
                    if (result.IsSuccess)
                    {
                        description += $"{cmd.Name}\n";
                    }
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.SlashGroupName;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }
            await FollowupAsync(ephemeral: true, embed: builder.Build());
        }
    }
}