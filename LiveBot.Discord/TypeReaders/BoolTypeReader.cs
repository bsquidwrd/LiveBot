using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace LiveBot.Discord.TypeReaders
{
    internal class BoolTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext Context, string Input, IServiceProvider Services)
        {
            switch (Input.ToLowerInvariant())
            {
                case "on":
                    return Task.FromResult(TypeReaderResult.FromSuccess(true));

                case "off":
                    return Task.FromResult(TypeReaderResult.FromSuccess(false));

                case "yes":
                    return Task.FromResult(TypeReaderResult.FromSuccess(true));

                case "no":
                    return Task.FromResult(TypeReaderResult.FromSuccess(false));
            }
            bool Result;
            if (bool.TryParse(Input, out Result))
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(Result));
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed,
                "Input could not be parsed as a boolean."));
        }
    }
}