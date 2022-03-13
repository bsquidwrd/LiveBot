using Discord;
using Discord.Interactions;
using System.Text.RegularExpressions;

namespace LiveBot.Discord.SlashCommands.Helpers
{
    public class UriConverter : TypeConverter<Uri>
    {
        public override ApplicationCommandOptionType GetDiscordType() => ApplicationCommandOptionType.String;

        public override Task<TypeConverterResult> ReadAsync(IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        {
            if (option.Value is string uriString)
            {
                if (uriString != null)
                    return Task.FromResult(TypeConverterResult.FromSuccess(new Uri(uriString: uriString)));
            }
            context.Interaction.RespondAsync(text: $"Could not convert your input to a proper URL. Please try again", ephemeral: true);
            throw new Exception("Could not parse input string to URL");
        }
    }

    public static class UriUtils
    {
        public static Uri? FindFirstUri(string input)
        {
            const string URLPattern = "(ht|f)tp(s?)\\:\\/\\/[0-9a-zA-Z]([-.\\w]*[0-9a-zA-Z])*(:(0-9)*)*(\\/?)([a-zA-Z0-9\\-\\.\\?\\,\'\\/\\\\\\+&%\\$#_]*)?";
            var URLRegex = new Regex(URLPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            if (Regex.IsMatch(input, URLPattern))
            {
                var URLMatch = URLRegex.Match(input);
                var firstURL = URLMatch.Groups[0].ToString();
                return new Uri(firstURL);
            }
            else
            {
                return null;
            }
        }
    }
}