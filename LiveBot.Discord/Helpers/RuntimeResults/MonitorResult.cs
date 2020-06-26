using Discord.Commands;

namespace LiveBot.Discord.Helpers.RuntimeResults
{
    internal class MonitorResult : RuntimeResult
    {
        public MonitorResult(CommandError? error, string reason) : base(error, reason)
        {
        }

        public static MonitorResult FromError(string reason) =>
            new MonitorResult(CommandError.Unsuccessful, reason);

        public static MonitorResult FromSuccess(string reason = null) =>
            new MonitorResult(null, reason);
    }
}