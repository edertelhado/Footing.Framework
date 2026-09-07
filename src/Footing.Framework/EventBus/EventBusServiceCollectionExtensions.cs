using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
namespace Footing.Framework.EventBus;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services, int workerCount = 4)
    {
        var options = new EventBusOptions { WorkerCount = workerCount };
        services.AddSingleton(sp => new EventBus(options, sp.GetService<ILogger<EventBus>>()));
        services.AddHostedService<EventBusHostedService>();

        RegisterEventHandlers(services);

        return services;
    }

    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        Action<EventBusOptions> configureEventBus)
    {
        var options = new EventBusOptions();
        configureEventBus(options);

        services.AddSingleton(sp => new EventBus(options, sp.GetService<ILogger<EventBus>>()));
        services.AddHostedService<EventBusHostedService>();

        if (options.AutoRegisterHandlers)
            RegisterEventHandlers(services);

        return services;
    }

    private static void RegisterEventHandlers(IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException) { continue; }

            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsInterface) continue;

                var handlerInterfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType
                        && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

                foreach (var handlerInterface in handlerInterfaces)
                {
                    services.TryAddTransient(handlerInterface, type);
                }
            }
        }
    }
}

public class EventBusOptions
{
    public int WorkerCount { get; set; } = 4;
    public int Capacity { get; set; } = 1000;
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;
    public bool DrainOnStop { get; set; } = true;
    public bool EnableDetailedLogging { get; set; } = false;
    public bool AutoRegisterHandlers { get; set; } = true;
}
