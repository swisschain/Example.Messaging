﻿using System;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Publisher.Common.Configuration;
using Publisher.Common.HostedServices;
using Publisher.Worker.HostedServices;
using Swisschain.Sdk.Server.Common;

namespace Publisher.Worker
{
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            base.ConfigureServicesExt(services);

            services.AddMassTransit(x =>
            {
                x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    cfg.Host(Config.RabbitMq.HostUrl, host =>
                    {
                        host.Username(Config.RabbitMq.Username);
                        host.Password(Config.RabbitMq.Password);
                    });

                    cfg.UseMessageRetry(y =>
                        y.Exponential(5,
                            TimeSpan.FromMilliseconds(100),
                            TimeSpan.FromMilliseconds(10_000),
                            TimeSpan.FromMilliseconds(100)));

                    cfg.SetLoggerFactory(provider.GetRequiredService<ILoggerFactory>());
                }));

                services.AddHostedService<BusHost>();
            });

            services.AddHostedService<TimedHostedService>();
        }
    }
}
