using Discord.Commands;
using LiveBot.Core.Repository.Interfaces.Monitor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LiveBot.Discord.Helpers
{
    public class LiveBotUserTypeReader : TypeReader
    {
        /// <inheritdoc/>
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext Context, string Input, IServiceProvider Services)
        {
            ILiveBotUser liveBotUser;
            Input = Input.Trim();

            const string URLPattern = "^(ht|f)tp(s?)\\:\\/\\/[0-9a-zA-Z]([-.\\w]*[0-9a-zA-Z])*(:(0-9)*)*(\\/?)([a-zA-Z0-9\\-\\.\\?\\,\'\\/\\\\\\+&%\\$#_]*)?$";
            Regex URLRegex = new Regex(URLPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // Check if Valid URL
            if (Regex.IsMatch(Input, URLPattern))
            {
                Match URLMatch = URLRegex.Match(Input);
                IEnumerable<ILiveBotMonitor> monitors = Services.GetServices<ILiveBotMonitor>();
                ILiveBotMonitor monitor = monitors.Where(m => m.IsValid(URLMatch.Groups[0].ToString())).FirstOrDefault();

                if (monitor == null)
                    return TypeReaderResult.FromError(CommandError.Unsuccessful,
                        $"{Context.Message.Author.Mention}, I couldn't process the link you provided. Please check the link and try again.");

                liveBotUser = await monitor.GetUser(profileURL: Input);
            }
            else
            {
                return TypeReaderResult.FromError(CommandError.ParseFailed,
                    $"{Context.Message.Author.Mention}, you must provide a valid link to the stream you want to monitor.");
            }

            if (liveBotUser == null)
            {
                return TypeReaderResult.FromError(CommandError.ParseFailed,
                    $"{Context.Message.Author.Mention}, I couldn't determine what type of stream that was.");
            }

            return TypeReaderResult.FromSuccess(liveBotUser);
        }
    }
}