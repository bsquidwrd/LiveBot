using LiveBot.Core.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace LiveBot.Repository
{
    public class LiveBotDBContext : DbContext
    {
        public LiveBotDBContext() : base()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("LiveBotConnectionString"));
        }

        public DbSet<DiscordGuild> DiscordGuild { get; set; }
        public DbSet<DiscordChannel> DiscordChannel { get; set; }
    }
}