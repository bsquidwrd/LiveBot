using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models.Streams;
using LiveBot.Core.Repository.Static;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Core.Consumers
{
    public class MonitorUpdateUserConsumer : IConsumer<IMonitorUpdateUser>
    {
        private readonly IBusControl _bus;
        private readonly List<ILiveBotMonitor> _monitors;
        private readonly IUnitOfWork _work;

        public MonitorUpdateUserConsumer(IBusControl bus, List<ILiveBotMonitor> monitors, IUnitOfWorkFactory factory)
        {
            _bus = bus;
            _monitors = monitors;
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IMonitorUpdateUser> context)
        {
            IMonitorUpdateUser monitorUpdateUser = context.Message;
            ILiveBotUser user = monitorUpdateUser.User;
            ServiceEnum ServiceType = monitorUpdateUser.ServiceType;
            ILiveBotMonitor monitor = _monitors.Where(i => i.ServiceType == ServiceType).FirstOrDefault();

            if (monitor == null)
                return;

            StreamUser existingUser = await _work.UserRepository.SingleOrDefaultAsync(i => i.ServiceType == ServiceType && i.SourceID == user.Id);

            if (existingUser != null)
            {
                if (DateTime.UtcNow.Subtract(existingUser.TimeStamp).TotalHours > 1)
                {
                    ILiveBotUser apiUser = await monitor.GetUserById(user.Id);
                    StreamUser streamUser = new StreamUser()
                    {
                        ServiceType = ServiceType,
                        SourceID = apiUser.Id,
                        Username = apiUser.Username,
                        DisplayName = apiUser.DisplayName,
                        AvatarURL = apiUser.AvatarURL,
                        ProfileURL = apiUser.ProfileURL
                    };
                    await _work.UserRepository.AddOrUpdateAsync(streamUser, (i => i.ServiceType == ServiceType && i.SourceID == user.Id));
                }
            }
        }
    }
}