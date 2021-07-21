using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Discord.Modules
{
    /// <summary>
    /// Location of all the "Help" commands
    /// </summary>
    [RequireBotPermission(ChannelPermission.EmbedLinks)]
    public class HelpModule : ModuleBase<ShardedCommandContext>
    {
        private readonly CommandService _service;

        public HelpModule(CommandService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gives a list of all loaded commands and their full invocations
        /// </summary>
        /// <returns></returns>
        [Command("help")]
        public async Task HelpAsync()
        {
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use"
            };

            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                    {
                        description += $"{cmd.Aliases.First()}\n";
                    }
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            await ReplyAsync("", false, builder.Build());
        }

        /// <summary>
        /// Gets help for the given <paramref name="command"/>
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [Command("help")]
        public async Task HelpAsync(string command)
        {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                              $"Summary: {cmd.Summary}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }
    }
}