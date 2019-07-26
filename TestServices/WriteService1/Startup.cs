﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microwave;
using Microwave.EventStores.SnapShots;
using Microwave.Persistence.MongoDb;
using Microwave.UI;
using ReadService1;
using ServerConfig;

namespace WriteService1
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(config =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new ApiKeyRequirement())
                    .Build();
                config.Filters.Add(new AuthorizeFilter(policy));
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddMicrowaveUi();

            services.AddMicrowave(config =>
            {
                config.AddServiceName("WriteService1");
                config.ServiceLocations.AddRange(ServiceConfiguration.ServiceAdresses);
                config.AddHttpClientCreator(new MyMicrowaveHttpClientCreator());
                config.SnapShots.Add(new SnapShot<EntityTest>(3));
            });

            services.AddMicrowavePersistenceLayerMongoDb(p =>
            {
                p.WithDatabaseName("TestWriteService1ReadDb");
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc();
            app.UseMicrowaveUi();
            app.RunMicrowaveQueries();
        }
    }
}