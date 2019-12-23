﻿using LiveBot.Core.Repository;
using Microsoft.EntityFrameworkCore;
using System;

namespace LiveBot.Repository
{
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly DbContextOptions _options;

        public UnitOfWorkFactory(string conString)
        {
            if (string.IsNullOrWhiteSpace(conString))
                throw new ArgumentNullException(nameof(conString));

            var optionBuilder = new DbContextOptionsBuilder();
            optionBuilder.UseNpgsql(conString);

            _options = optionBuilder.Options;
        }

        public IUnitOfWork Create()
        {
            return new UnitOfWork(new LiveBotDBContext(_options));
        }
    }
}