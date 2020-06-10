using LiveBot.Core.Repository.Static;
using System;

namespace LiveBot.Core.Repository.Models.Streams
{
    public class StreamNotification : BaseModel<StreamNotification>
    {
        public ServiceEnum ServiceType { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public ulong DiscordMessage_DiscordId { get; set; }

        public string User_SourceID { get; set; }
        public string User_Username { get; set; }
        public string User_DisplayName { get; set; }
        public string User_AvatarURL { get; set; }
        public string User_ProfileURL { get; set; }

        public string Stream_SourceID { get; set; }
        public string Stream_Title { get; set; }
        public DateTime Stream_StartTime { get; set; }
        public string Stream_ThumbnailURL { get; set; }
        public string Stream_StreamURL { get; set; }

        public string Game_SourceID { get; set; }
        public string Game_Name { get; set; }
        public string Game_ThumbnailURL { get; set; }

        public ulong DiscordGuild_DiscordId { get; set; }
        public string DiscordGuild_Name { get; set; }

        public ulong DiscordChannel_DiscordId { get; set; }
        public string DiscordChannel_Name { get; set; }

        public ulong DiscordRole_DiscordId { get; set; }
        public string DiscordRole_Name { get; set; }
    }
}