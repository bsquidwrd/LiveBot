using LiveBot.Core.Contracts;
using LiveBot.Core.Repository.Interfaces.Monitor;
using MassTransit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LiveBot.Core.Consumers
{
    public class MonitorUpdateUsersConsumer : IConsumer<IMonitorUpdateUsers>
    {
        private readonly List<ILiveBotMonitor> _monitors;
        private readonly IBusControl _bus;

        public MonitorUpdateUsersConsumer(List<ILiveBotMonitor> monitors, IBusControl bus)
        {
            _monitors = monitors;
            _bus = bus;
        }

        public async Task Consume(ConsumeContext<IMonitorUpdateUsers> context)
        {
            IMonitorUpdateUsers message = context.Message;
            ILiveBotMonitor monitor = _monitors.Where(i => i.ServiceType == message.ServiceType).FirstOrDefault();

            if (monitor == null)
                return;

            await monitor.UpdateUsers();

            await Task.Delay(5 * 1000); // 5 minutes
            await _bus.Publish(message);
        }
    }
}