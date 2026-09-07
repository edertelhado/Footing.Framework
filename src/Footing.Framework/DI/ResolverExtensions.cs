using System.Collections.Concurrent;
using System.Reflection;
using Footing.Framework.Config;
using Footing.Framework.EventBus;
using Footing.Framework.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Footing.Framework.DI;

public static class ResolverExtensions
{
    private static readonly ConcurrentDictionary<(Type, MethodInfo), bool> _registeredListeners = new();
    private static readonly ConcurrentDictionary<Type, bool> _registeredTypes = new();
    public static void RegisterType(
        IServiceCollection services,
        Type type,
        InjectableLifetime lifetime,
        string rootNamespace,
        IConfiguration? config = null,
        ILogger? logger = null)
    {
        var interfaces = type.GetInterfaces();

        var targets = interfaces
            .Where(i => i.Namespace != null
                        && i.Namespace.StartsWith(rootNamespace, StringComparison.Ordinal)
                        && !typeof(IDisposable).IsAssignableFrom(i)
                        && !typeof(IAsyncDisposable).IsAssignableFrom(i))
            .Concat([type])
            .Distinct();

        foreach (var target in targets.Distinct())
        {
            switch (lifetime)
            {
                case InjectableLifetime.Singleton:
                    services.AddSingleton(target, provider =>
                    {
                        var instance = CreateInstanceWithLifecycle(provider, type, config);
                        logger?.LogDebug("[Singleton] Registering {TypeName} as {TargetName}", type.FullName, target.FullName);
                        return UnwrapForConcreteTarget(instance, target, type);
                    });
                    break;

                case InjectableLifetime.Scoped:
                    services.AddScoped(target, provider =>
                    {
                        var instance = CreateInstanceWithLifecycle(provider, type, config);
                        logger?.LogDebug("[Scoped] Registering {TypeName} as {TargetName}", type.FullName, target.FullName);
                        return UnwrapForConcreteTarget(instance, target, type);
                    });
                    break;

                case InjectableLifetime.Transient:
                    services.AddTransient(target, provider =>
                    {
                        var instance = CreateInstanceWithLifecycle(provider, type, config);
                        logger?.LogDebug("[Transient] Registering {TypeName} as {TargetName}", type.FullName, target.FullName);
                        return UnwrapForConcreteTarget(instance, target, type);
                    });
                    break;
            }
        }
    }

    /// <summary>
    /// When target is the concrete type itself, returns the real instance instead of
    /// <see cref="LifecycleProxy"/> (which only makes sense for registrations via interface).
    /// Without this, an IDisposable/IAsyncDisposable service registered as concrete could not
    /// be resolved (cast LifecycleProxy → concrete type fails).
    /// </summary>
    private static object UnwrapForConcreteTarget(object instance, Type target, Type concreteType)
        => target == concreteType && instance is LifecycleProxy proxy ? proxy.Instance : instance;

    private static object CreateInstanceWithLifecycle(
        IServiceProvider provider,
        Type type,
        IConfiguration? config)
    {
        var instance = ActivatorUtilities.CreateInstance(provider, type);

        if (config != null)
            InjectConfigs(instance, config);

        ExecutePostConstruct(instance);
        RegisterEventListeners(provider, instance);

        var (preDestroy, preDestroyAsync) = FindPreDestroy(instance);
        var hasDispose = instance is IDisposable or IAsyncDisposable;
        var hasPreDestroy = preDestroy != null || preDestroyAsync != null;

        if (hasPreDestroy || hasDispose)
            return new LifecycleProxy(instance, preDestroy, preDestroyAsync);

        return instance;
    }

    private static void ExecutePostConstruct(object instance)
    {
        var methods = instance.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<PostConstructAttribute>() != null)
            .ToList();

        if (methods.Count == 0) return;
        if (methods.Count > 1)
            throw new InvalidOperationException($"Multiple [PostConstruct] methods found on {instance.GetType().Name}. Only one allowed.");

        var method = methods[0];

        try
        {
            var result = method.Invoke(instance, null);

            if (result is Task task)
            {
                try
                {
                    Task.Run(async () => await task.ConfigureAwait(false)).GetAwaiter().GetResult();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException ?? ae;
                }
            }
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw new InvalidOperationException(
                $"Error executing [PostConstruct] on {instance.GetType().Name}", ex.InnerException);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error executing [PostConstruct] on {instance.GetType().Name}", ex);
        }
    }

    private static (MethodInfo? Sync, MethodInfo? Async) FindPreDestroy(object instance)
    {
        var methods = instance.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<PreDestroyAttribute>() != null)
            .ToList();

        if (methods.Count == 0) return (null, null);
        if (methods.Count > 1)
            throw new InvalidOperationException($"Multiple [PreDestroy] methods found on {instance.GetType().Name}. Only one allowed.");

        var sync = methods.FirstOrDefault(m => m.ReturnType == typeof(void));
        var async = methods.FirstOrDefault(m => m.ReturnType == typeof(Task));

        return (sync, async);
    }

    private static void RegisterEventListeners(IServiceProvider provider, object instance)
    {
        var eventBus = provider.GetService<EventBus.EventBus>();
        if (eventBus == null) return;

        var type = instance.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<EventListenerAttribute>() != null)
            .ToList();

        var handlerInterfaces = type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(EventBus.IEventHandler<>))
            .ToList();

        if (methods.Count == 0 && handlerInterfaces.Count == 0) return;

        bool hasNew = false;
        foreach (var method in methods)
        {
            if (_registeredListeners.TryAdd((type, method), true))
                hasNew = true;
        }

        if (handlerInterfaces.Count > 0)
        {
            if (_registeredTypes.TryAdd(type, true))
                hasNew = true;
        }

        if (!hasNew) return;

        eventBus.RegisterListenersFromInstance(instance);
    }

    private static void InjectConfigs(object instance, IConfiguration config)
    {
        var props = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<InjectConfigAttribute>() != null);

        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<InjectConfigAttribute>()!;
            var value = config.GetValue(prop.PropertyType, attr.Section);
            prop.SetValue(instance, value);
        }
    }
}
