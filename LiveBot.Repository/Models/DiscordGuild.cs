using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LiveBot.Repository.Models
{
    public class DiscordGuild
    {
        [Key]
        public ulong Id { get; set; }
        public string Name { get; set; }
    }
}