using System;

namespace LiveBot.Core.Repository.Static
{
    public class Queues
    {
        private static readonly string _queueHost = Environment.GetEnvironmentVariable("RabbitMQ_URL")
            ?? throw new InvalidOperationException("Required environment variable 'RabbitMQ_URL' is not set.");

        public static string QueueURL => $"rabbitmq://{_queueHost}";

        public static readonly string QueueUsername = Environment.GetEnvironmentVariable("RabbitMQ_Username")
            ?? throw new InvalidOperationException("Required environment variable 'RabbitMQ_Username' is not set.");
        public static readonly string QueuePassword = Environment.GetEnvironmentVariable("RabbitMQ_Password")
            ?? throw new InvalidOperationException("Required environment variable 'RabbitMQ_Password' is not set.");

        public static readonly ushort PrefetchCount = 32;

        public static readonly string DiscordAlert = "discord_alert";
        public static readonly string DiscordAlertChannel = "discord_alertchannel";

        public static readonly string DiscordGuildAvailable = "discord_guildavailable";
        public static readonly string DiscordGuildUpdate = "discord_guildupdate";
        public static readonly string DiscordGuildDelete = "discord_guilddelete";
        public static readonly string DiscordChannelUpdate = "discord_channelupdate";
        public static readonly string DiscordChannelDelete = "discord_channeldelete";
        public static readonly string DiscordRoleDelete = "discord_roledelete";

        public static readonly string DiscordMemberLive = "discord_memberlive";

        public static readonly string StreamOnlineQueueName = "streamonline";
        public static readonly string StreamUpdateQueueName = "streamupdate";
        public static readonly string StreamOfflineQueueName = "streamoffline";

        // Request/Response: on-demand stream status checks per service
        public static string GetStreamCheckQueueName(ServiceEnum service)
            => service switch
            {
                ServiceEnum.Twitch => "streamcheck.twitch",
                ServiceEnum.YouTube => "streamcheck.youtube",
                ServiceEnum.Trovo => "streamcheck.trovo",
                _ => "streamcheck.unknown"
            };
    }
}