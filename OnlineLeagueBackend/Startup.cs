﻿using Adapters.Framework.WebApi;
using Adapters.WebApi.Seasons;
using Application.Framework;
using Application.Seasons;
using Application.Seasons.Querries;
using DependencyInjection.Framework;
using Domain.Seasons.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OnlineLeagueBackend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddTransient<SeasonController>();
            services.AddTransient<StartEventUpdateController>();
            services.AddTransient<DomainEventController>();

            services.AddTransient<SeasonCommandHandler>();
            services.AddTransient<AllSeasonsQuery>();
            services.AddTransient<AllSeasonsCounterQuery>();

            services.AddTransient<IEventDelegateHandler, EventDelegateHandler<SeasonCreatedEvent>>();
            services.AddTransient<IEventDelegateHandler, EventDelegateHandler<SeasonNameChangedEvent>>();

            services.AddTransient<IHandleAsync<SeasonCreatedEvent>, SeasonCreatedEventHandler>();
            services.AddTransient<IHandleAsync<SeasonNameChangedEvent>, SeasonChangedNamEventHandler>();
            services.AddTransient<IHandleAsync<SeasonNameChangedEvent>, SeasonCreatedEventHandler>();

            services.AddTransient<IHandleAsync<SeasonCreatedEvent>, Top10NameChangesEventHandler>();
            services.AddTransient<IHandleAsync<SeasonNameChangedEvent>, Top10NameChangesEventHandler>();

            services.AddTransient<IEventFeed<SeasonCreatedEvent>, EventFeed<SeasonCreatedEvent>>();
            services.AddTransient<IEventFeed<SeasonNameChangedEvent>, EventFeed<SeasonNameChangedEvent>>();

            services.AddTransient<DomainEventClient<SeasonCreatedEvent>>();
            services.AddTransient<DomainEventClient<SeasonNameChangedEvent>>();

            services.AddTransient<IQueryEventHandler, QueryEventHandler<AllSeasonsQuery, SeasonCreatedEvent>>();
            services.AddTransient<IQueryEventHandler, QueryEventHandler<AllSeasonsQuery, SeasonNameChangedEvent>>();

            services.AddTransient<IQueryEventHandler, QueryEventHandler<AllSeasonsCounterQuery, SeasonCreatedEvent>>();

            services.AddTransient<IIdentifiableQueryEventHandler, IdentifiableQueryEventHandler<SingleSeasonsQuery, SeasonCreatedEvent>>();
            services.AddTransient<IIdentifiableQueryEventHandler, IdentifiableQueryEventHandler<SingleSeasonsQuery, SeasonNameChangedEvent>>();

            services.AddMyEventStoreDependencies(typeof(AllSeasonsCounterQuery).Assembly, Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}