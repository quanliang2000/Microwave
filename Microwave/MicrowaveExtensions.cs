﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microwave.EventStores;
using Microwave.Logging;
using Microwave.Queries;
using Microwave.Queries.Handler;
using Microwave.Queries.Ports;

namespace Microwave
{
    public static class MicrowaveExtensions
    {
        private static Type _genericTypeOfFeed;

        public static IServiceCollection AddMicrowave(
            this IServiceCollection services,
            Action<MicrowaveConfiguration> addConfiguration)
        {
            var microwaveConfiguration = new MicrowaveConfiguration();
            addConfiguration.Invoke(microwaveConfiguration);
            _genericTypeOfFeed = microwaveConfiguration.FeedType;

            var assemblies = GetAllAssemblies();

            services.AddTransient<IEventStore, EventStore>();

            services.AddSingleton(typeof(IMicrowaveLogger<>), typeof(MicrowaveLogger<>));
            services.AddSingleton(microwaveConfiguration.LogLevel);


            foreach (var assembly in assemblies)
            {
                services.AddQuerryHandling(assembly);
                services.AddAsyncEventHandling(assembly);
                services.AddReadmodelHandling(assembly);
            }

            return services;
        }

        private static List<Assembly> GetAllAssemblies()
        {
            var assemblies = new List<Assembly>();
            var referencedPaths = Directory
                .GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll", SearchOption.AllDirectories).ToList();
            referencedPaths.ForEach(path =>
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(path);
                    assemblies.Add(AppDomain.CurrentDomain.Load(assemblyName));
                }
                catch (FileNotFoundException)
                {
                }
            });
            return assemblies;
        }

        private static IServiceCollection AddQuerryHandling(this IServiceCollection services, Assembly assembly)
        {
            var queryInterfaces = assembly.GetTypes().Where(ImplementsIhandleInterfaceAndQuerry);
            var genericInterfaceTypeOfFeed = typeof(IEventFeed<>);
            var genericTypeOfHandler = typeof(QueryEventHandler<,>);

            foreach (var query in queryInterfaces)
            {
                var types = query.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandle<>));
                foreach (var iHandleEvent in types)
                {
                    //feed
                    var domainEventType = iHandleEvent.GenericTypeArguments.Single();
                    var genericHandler = genericTypeOfHandler.MakeGenericType(query, domainEventType);
                    var feed = _genericTypeOfFeed.MakeGenericType(genericHandler);
                    var feedInterface = genericInterfaceTypeOfFeed.MakeGenericType(genericHandler);
                    services.AddTransient(feedInterface, feed);

                    //handler
                    services.AddSingleton(genericHandler);
                    services.AddSingleton(provider =>
                    {
                        var requiredService = provider.GetRequiredService(genericHandler) as IQueryEventHandler;
                        return new QueryBackgroundService(requiredService);
                    });
                }
            }

            return services;
        }

        private static IServiceCollection AddAsyncEventHandling(this IServiceCollection services, Assembly
        assembly)
        {
            var handleAsyncInterfaces = assembly.GetTypes().Where(ImplementsIhandleAsyncInterface);
            var genericInterfaceTypeOfFeed = typeof(IEventFeed<>);
            var genericTypeOfHandler = typeof(AsyncEventHandler<>);
            var handleAsyncType = typeof(IHandleAsync<>);

            foreach (var handleAsync in handleAsyncInterfaces)
            {
                var types = handleAsync.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleAsync<>));
                services.AddTransient(handleAsync);

                foreach (var iHandleEvent in types)
                {
                    //feed
                    var domainEventType = iHandleEvent.GenericTypeArguments.Single();
                    var genericHandler = genericTypeOfHandler.MakeGenericType(domainEventType);
                    var feed = _genericTypeOfFeed.MakeGenericType(genericHandler);
                    var feedInterface = genericInterfaceTypeOfFeed.MakeGenericType(genericHandler);
                    services.AddTransient(feedInterface, feed);

                    //handler
                    services.AddSingleton(s =>
                    {
                        var versionRepo = s.GetRequiredService<IVersionRepository>();
                        var feedInstance = s.GetRequiredService(feedInterface);
                        var handleAsyncInstance = s.GetRequiredService(handleAsync);
                        var constructorInfo = genericHandler.GetConstructors().Single();
                        var createdHandlerInstance = constructorInfo.Invoke(new [] { versionRepo, feedInstance, handleAsyncInstance });

                        return createdHandlerInstance;

                    });
                    services.AddSingleton(provider =>
                    {
                        var requiredService = provider.GetRequiredService(genericHandler) as IAsyncEventHandler;
                        return new AsyncEventBackgroundService(requiredService);
                    });


                    //handleAsyncs
                    var handleAsyncTypeWithEvent = handleAsyncType.MakeGenericType(domainEventType);
                    services.AddTransient(handleAsyncTypeWithEvent, handleAsync);
                }
            }

            return services;
        }

        private static IServiceCollection AddReadmodelHandling(this IServiceCollection services, Assembly assembly)
        {
            var readModels = assembly.GetTypes().Where(ImplementsIhandleInterfaceAndReadModel).ToList();
            var genericInterfaceTypeOfFeed = typeof(IEventFeed<>);
            var genericTypeOfHandler = typeof(ReadModelEventHandler<>);

            foreach (var readModel in readModels)
            {
                var genericReadModelHandler = genericTypeOfHandler.MakeGenericType(readModel);
                //feed
                var genericHandler = genericReadModelHandler;
                var feed = _genericTypeOfFeed.MakeGenericType(genericHandler);
                var feedInterface = genericInterfaceTypeOfFeed.MakeGenericType(genericHandler);
                services.AddTransient(feedInterface, feed);

                //handler
                services.AddSingleton(genericReadModelHandler);
                services.AddSingleton(provider =>
                {
                    var requiredService = provider.GetRequiredService(genericReadModelHandler) as IReadModelEventHandler;
                    return new ReadModelBackgroundService(requiredService);
                });
            }

            return services;
        }

        private static bool ImplementsIhandleAsyncInterface(Type myType)
        {
            return myType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleAsync<>));
        }

        private static bool ImplementsIhandleInterfaceAndQuerry(Type myType)
        {
            return myType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandle<>)) && myType.BaseType == typeof(Query);
        }

        private static bool ImplementsIhandleInterfaceAndReadModel(Type myType)
        {
            return myType.GetInterfaces()
                       .Any(i => i.IsGenericType
                                 && i.GetGenericTypeDefinition() == typeof(IHandle<>))
                                 && typeof(ReadModelBase).IsAssignableFrom(myType);
        }
    }
}