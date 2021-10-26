using Elsa;
using Elsa.Runtime;
using System;
using Elsa.Options;
using Elsa.Services;
using AppBroker.Elsa.Signaler;
using AppBroker.Elsa.Bookmarks;
using AppBroker.Elsa.Activities;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPropetyActivities(this IServiceCollection services)
        {
            return services;
        }

        public static ElsaOptionsBuilder AddPropetyActivities(this ElsaOptionsBuilder builder) 
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            _ = builder
                .AddActivity<PropertyChangedTrigger>()
                .AddActivity<DeviceChangedTrigger>();

            _ = builder.Services
                .AddSingleton<Scoped<IWorkflowLaunchpad>>()
                .AddBookmarkProvider<PropertyChangedEventBookmarkProvider>()
                .AddBookmarkProvider<DeviceChangedEventBookmarkProvider>()
                .AddSingleton<WorkflowDeviceSignaler>()
                .AddSingleton<WorkflowPropertySignaler>();

            return builder;
        }
    }
}
