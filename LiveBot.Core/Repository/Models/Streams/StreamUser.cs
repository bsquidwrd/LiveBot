﻿using System.Collections.Generic;
using LiveBot.Core.Repository.Static;
using Newtonsoft.Json;

namespace LiveBot.Core.Repository.Models.Streams
{
    public class StreamUser : BaseModel<StreamUser>
    {
        public ServiceEnum ServiceType { get; set; }
        public string SourceID { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string AvatarURL { get; set; }
        public string ProfileURL { get; set; }

        [JsonIgnore]
        public virtual ICollection<StreamSubscription> StreamSubscriptions { get; set; }
    }
}