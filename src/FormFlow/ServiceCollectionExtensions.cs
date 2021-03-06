﻿using System;
using System.Linq;
using System.Reflection;
using FormFlow.Filters;
using FormFlow.State;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FormFlow
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFormFlow(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHttpContextAccessor();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<JourneyInstanceProvider>();
            services.TryAddSingleton<IStateSerializer, JsonStateSerializer>();
            services.TryAddSingleton<IUserInstanceStateProvider, UserInstanceStateProvider>();
            services.TryAddSingleton<IUserInstanceStateStore, SessionUserInstanceStateStore>();
            services.AddSingleton<IConfigureOptions<FormFlowOptions>, FormFlowOptionsSetup>();

            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new MissingInstanceActionFilter());
            });

            return services;
        }

        public static IServiceCollection AddFormFlow(
            this IServiceCollection services,
            Action<FormFlowOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.Configure(configure);
            services.AddFormFlow();

            return services;
        }

        public static IServiceCollection AddJourneyStateTypes(
            this IServiceCollection services,
            Assembly assembly)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var stateTypes = assembly.GetTypes()
                .Where(t => t.IsPublic && !t.IsAbstract && t.GetCustomAttribute<JourneyStateAttribute>() != null);

            foreach (var type in stateTypes)
            {
                var instanceType = typeof(JourneyInstance<>).MakeGenericType(type);

                services.AddTransient(instanceType, sp =>
                {
                    var instanceProvider = sp.GetRequiredService<JourneyInstanceProvider>();
                    return instanceProvider.GetInstance();
                });
            }

            return services;
        }

        public static IServiceCollection AddFormFlowStateTypes(
            this IServiceCollection services,
            Type fromAssemblyContainingType)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (fromAssemblyContainingType == null)
            {
                throw new ArgumentNullException(nameof(fromAssemblyContainingType));
            }

            return AddJourneyStateTypes(services, fromAssemblyContainingType.Assembly);
        }
    }
}
