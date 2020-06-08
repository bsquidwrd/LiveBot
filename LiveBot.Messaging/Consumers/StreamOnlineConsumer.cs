using LiveBot.Messaging.Services;
using MassTransit;
using System.Threading.Tasks;

namespace LiveBot.Messaging.Consumers
{
    public class StreamOnlineConsumer : IConsumer<StreamOnline>
    {
        public async Task Consume(ConsumeContext<StreamOnline> context)
        {
        }
    }
}