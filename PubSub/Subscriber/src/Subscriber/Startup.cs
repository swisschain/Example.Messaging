﻿using System.IO;
using System.Reflection;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Subscriber.Common.Configuration;
using Subscriber.Common.HostedServices;
using Subscriber.GrpcServices;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swisschain.Sdk.Server.Common;

namespace Subscriber
{
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            base.ConfigureServicesExt(services);

            services.AddPersistence(Config.Db.ConnectionString);
            services.AddAppFeatureExample();

            services.AddMassTransit(x =>
            {
                x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    cfg.Host(Config.RabbitMq.HostUrl, host =>
                    {
                        host.Username(Config.RabbitMq.Username);
                        host.Password(Config.RabbitMq.Password);
                    });

                    cfg.SetLoggerFactory(provider.GetRequiredService<ILoggerFactory>());
                }));

                services.AddHostedService<BusHost>();
            });
        }

        protected override void ConfigureSwaggerGen(SwaggerGenOptions swaggerGenOptions)
        {
            base.ConfigureSwaggerGen(swaggerGenOptions);

            var binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            swaggerGenOptions.IncludeXmlComments($"{binPath}/Subscriber.xml", includeControllerXmlComments: true);
        }

        protected override void RegisterEndpoints(IEndpointRouteBuilder endpoints)
        {
            base.RegisterEndpoints(endpoints);

            endpoints.MapGrpcService<MonitoringService>();
        }
    }
}