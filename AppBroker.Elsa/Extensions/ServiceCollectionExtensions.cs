using Elsa;
using Elsa.Runtime;
using System;
using Elsa.Options;
using Elsa.Services;
using AppBroker.Elsa.Signaler;
using AppBroker.Elsa.Bookmarks;
using AppBroker.Activities;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPropetyActivities(this IServiceCollection services) => services;

    public static ElsaOptionsBuilder AddPropertyActivities(this ElsaOptionsBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        _ = builder
            .AddActivity<PropertyChangedTrigger>()
            .AddActivity<GetDeviceActivity>()
            .AddActivity<GetDevicesActivity>()
            .AddActivity<DeviceChangedTrigger>();

        _ = builder.Services
            .AddSingleton<IWorkflowLaunchpad>()
            .AddBookmarkProvider<PropertyChangedEventBookmarkProvider>()
            .AddBookmarkProvider<DeviceChangedEventBookmarkProvider>()
            .AddSingleton<WorkflowDeviceSignaler>()
            .AddSingleton<WorkflowPropertySignaler>();

        return builder;
    }
}
