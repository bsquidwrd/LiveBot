using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LiveBot.Repository.Models
{
    public class DiscordGuild
    {
        [Key]
        public ulong GuildID { get; set; }
        public string GuildName { get; set; }
    }
}