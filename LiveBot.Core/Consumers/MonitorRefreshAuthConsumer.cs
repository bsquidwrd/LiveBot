using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces;
using LiveBot.Core.Repository.Interfaces.Monitor;
using LiveBot.Core.Repository.Models;
using MassTransit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Core.Consumers
{
    public class MonitorRefreshAuthConsumer : IConsumer<IMonitorRefreshAuth>
    {
        private readonly IBusControl _bus;
        private readonly List<ILiveBotMonitor> _monitors;
        private readonly IUnitOfWork _work;

        public MonitorRefreshAuthConsumer(IBusControl bus, List<ILiveBotMonitor> monitors, IUnitOfWorkFactory factory)
        {
            _bus = bus;
            _monitors = monitors;
            _work = factory.Create();
        }

        public async Task Consume(ConsumeContext<IMonitorRefreshAuth> context)
        {
            IMonitorRefreshAuth auth = context.Message;
            ILiveBotMonitor monitor = _monitors.Where(i => i.ServiceType == auth.ServiceType).FirstOrDefault();

            if (monitor == null)
                return;

            MonitorAuth oldMonitorAuth = await _work.AuthRepository.SingleOrDefaultAsync(i => i.ServiceType == auth.ServiceType && i.ClientId == auth.ClientId && i.Expired == false);
            MonitorAuth newMonitorAuth = await monitor.UpdateAuth(oldMonitorAuth);

            await _work.AuthRepository.AddOrUpdateAsync(newMonitorAuth, i => i.ServiceType == auth.ServiceType && i.ClientId == auth.ClientId && i.AccessToken == newMonitorAuth.AccessToken);

            oldMonitorAuth.Expired = true;
            await _work.AuthRepository.AddOrUpdateAsync(oldMonitorAuth, i => i.ServiceType == auth.ServiceType && i.ClientId == auth.ClientId && i.AccessToken == oldMonitorAuth.AccessToken);
        }
    }
}