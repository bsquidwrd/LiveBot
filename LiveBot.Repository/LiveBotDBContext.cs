﻿using LiveBot.Core.Repository;
using LiveBot.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace LiveBot.Repository
{
    public class LiveBotDBContext : DbContext, ILiveBotDBContext
    {
        public LiveBotDBContext(DbContextOptions options) : base(options)
        {
            Log.Debug("Database initialized");
        }

        public DbSet<DiscordGuild> DiscordGuild { get; set; }
    }
}