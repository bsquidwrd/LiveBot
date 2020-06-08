using LiveBot.Messaging.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace LiveBot.Messaging
{
    public class MessagingStart
    {
        public void PopulateServices(IServiceCollection services)
        {
            services.AddMassTransit(x =>
            {
                //x.AddConsumer<OrderConsumer>();

                x.AddBus(context => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    // configure health checks for this bus instance
                    cfg.UseHealthCheck(context);

                    cfg.Host(Environment.GetEnvironmentVariable("RabbitMQURL"));

                    //cfg.ReceiveEndpoint("submit-order", ep =>
                    //{
                    //    ep.PrefetchCount = 16;
                    //    ep.UseMessageRetry(r => r.Interval(2, 100));

                    //    ep.ConfigureConsumer<OrderConsumer>(context);
                    //});
                }));
            });

            services.AddMassTransitHostedService();
        }
    }
}