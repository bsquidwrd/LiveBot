﻿using System;

namespace LiveBot.Core.Repository.Static
{
    public class Queues
    {
        public static string QueueURL
        {
            get => $"rabbitmq://{Environment.GetEnvironmentVariable("RabbitMQ_URL")}";
        }

        public static string QueueUsername = Environment.GetEnvironmentVariable("RabbitMQ_Username");
        public static string QueuePassword = Environment.GetEnvironmentVariable("RabbitMQ_Password");

        public static ushort PrefetchCount = 32;

        public static string DiscordAlert = "discord_alert";
        public static string DiscordAlertChannel = "discord_alertchannel";

        public static string DiscordGuildAvailable = "discord_guildavailable";
        public static string DiscordGuildUpdate = "discord_guildupdate";
        public static string DiscordGuildDelete = "discord_guilddelete";
        public static string DiscordChannelUpdate = "discord_channelupdate";
        public static string DiscordChannelDelete = "discord_channeldelete";
        public static string DiscordRoleUpdate = "discord_roleupdate";
        public static string DiscordRoleDelete = "discord_roledelete";

        public static string DiscordMemberLive = "discord_memberlive";

        public static string StreamOnlineQueueName = "streamonline";
        public static string StreamUpdateQueueName = "streamupdate";
        public static string StreamOfflineQueueName = "streamoffline";
    }
}