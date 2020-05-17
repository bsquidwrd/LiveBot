using Discord.Commands;
using LiveBot.Discord.Services.LiveBot;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LiveBot.Discord.TypeReaders
{
    public class StreamChannelTypeReader : TypeReader
    {
        /// <inheritdoc />
        public override Task<TypeReaderResult> ReadAsync(ICommandContext Context, string Input, IServiceProvider Services)
        {
            BaseStreamChannel resolvedStreamChannel;
            Input = Input.Trim();

            const string URLPattern = "^(ht|f)tp(s?)\\:\\/\\/[0-9a-zA-Z]([-.\\w]*[0-9a-zA-Z])*(:(0-9)*)*(\\/?)([a-zA-Z0-9\\-\\.\\?\\,\'\\/\\\\\\+&%\\$#_]*)?$";
            Regex URLRegex = new Regex(URLPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // Check if Valid URL
            if (Regex.IsMatch(Input, URLPattern))
            {
                Match URLMatch = URLRegex.Match(Input);
                DetermineStreamChannel determineStreamChannel = new DetermineStreamChannel(URLMatch.Groups[0].ToString());
                resolvedStreamChannel = determineStreamChannel.Check();
            }
            else
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed,
                    $@"{Context.Message.Author.Mention}, you must provide a valid link to the stream you want to monitor."));
            }

            if (resolvedStreamChannel == null)
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed,
                    $@"{Context.Message.Author.Mention}, I couldn't determine what type of stream that was."));
            }

            return Task.FromResult(TypeReaderResult.FromSuccess(resolvedStreamChannel));
        }
    }
}