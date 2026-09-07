using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Footing.Framework.EventBus;

internal readonly struct PrioritizedHandler
{
    public readonly Func<object, Task> Handler;
    public readonly int Priority;

    public PrioritizedHandler(Func<object, Task> handler, int priority)
    {
        Handler = handler;
        Priority = priority;
    }
}

public class EventBus : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Type, List<PrioritizedHandler>> _handlers = new();
    private readonly Channel<object> _channel;
    private readonly List<Task> _workers = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly int _workerCount;
    private readonly ILogger<EventBus>? _logger;

    private int _listenersRegistered = 0;
    private readonly ConcurrentDictionary<Type, bool> _registeredTypes = new();
    private readonly ConcurrentDictionary<(Type, MethodInfo), bool> _registeredListeners = new();
    private readonly ConcurrentDictionary<(Type, Type), bool> _registeredHandlerInterfaces = new();

    private static readonly Comparer<PrioritizedHandler> PriorityComparer =
        Comparer<PrioritizedHandler>.Create((a, b) => b.Priority.CompareTo(a.Priority));

    private static readonly MethodInfo SubscribeTypedMethod = typeof(EventBus)
        .GetMethod(nameof(SubscribeTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public EventBus(int workerCount = 4, ILogger<EventBus>? logger = null)
        : this(new EventBusOptions { WorkerCount = workerCount }, logger)
    {
    }

    public EventBus(EventBusOptions options, ILogger<EventBus>? logger = null)
    {
        _workerCount = options.WorkerCount;
        _logger = logger;
        var boundedOptions = new BoundedChannelOptions(options.Capacity)
        {
            FullMode = options.FullMode,
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<object>(boundedOptions);
        for (int i = 0; i < _workerCount; i++)
            _workers.Add(Task.Run(() => WorkerLoop(_cts.Token)));
    }

    public void RegisterListenersFromProvider(IServiceProvider provider)
    {
        if (Interlocked.CompareExchange(ref _listenersRegistered, 1, 0) == 1)
        {
            _logger?.LogWarning("RegisterListenersFromProvider already called, skipping");
            return;
        }

        var serviceDescriptors = provider.GetService<IServiceCollection>();
        if (serviceDescriptors == null)
        {
            _logger?.LogError("IServiceCollection not found in provider");
            return;
        }

        foreach (var descriptor in serviceDescriptors)
        {
            if (descriptor.ServiceType == null) continue;
            if (!_registeredTypes.TryAdd(descriptor.ServiceType, true))
                continue;

            try
            {
                var service = provider.GetService(descriptor.ServiceType);
                if (service == null) continue;

                RegisterListenersFromInstance(service);
                RegisterTypedEventHandlerInterfaces(service);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
            {
                _logger?.LogWarning(ex, "Skipping service {ServiceType}", descriptor.ServiceType);
            }
        }
    }

    public void RegisterListenersFromInstance(object instance)
    {
        var type = instance.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<EventListenerAttribute>() != null);

        foreach (var method in methods)
        {
            if (!_registeredListeners.TryAdd((type, method), true))
                continue;

            var param = method.GetParameters().FirstOrDefault();
            if (param == null) continue;

            var eventType = param.ParameterType;
            var attr = method.GetCustomAttribute<EventListenerAttribute>()!;
            var priority = attr.Priority;

            var handler = CreateHandlerWrapper(type, method, instance);
            AddHandler(eventType, handler, priority);

            _logger?.LogDebug(
                "Registered listener {Type}.{Method} for event {Event} with priority {Priority}",
                type.Name, method.Name, eventType.Name, priority);
        }
    }

    private void RegisterTypedEventHandlerInterfaces(object service)
    {
        var handlerInterfaces = service.GetType()
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

        foreach (var handlerInterface in handlerInterfaces)
        {
            var key = (service.GetType(), handlerInterface);
            if (!_registeredHandlerInterfaces.TryAdd(key, true))
                continue;

            var eventType = handlerInterface.GetGenericArguments()[0];
            var method = SubscribeTypedMethod.MakeGenericMethod(eventType);
            method.Invoke(this, new object[] { service, 0 });

            _logger?.LogDebug(
                "Registered IEventHandler<{Event}> from {Service}",
                eventType.Name, service.GetType().Name);
        }
    }

    private void SubscribeTyped<TEvent>(IEventHandler<TEvent> handler, int priority)
    {
        Subscribe<TEvent>(e => handler.Handle(e, default), priority);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler, int priority = 0)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        AddHandler(typeof(TEvent), evt => handler((TEvent)evt), priority);
    }

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler, int priority = 0)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        Subscribe<TEvent>(e => handler.Handle(e, default), priority);
    }

    private void AddHandler(Type eventType, Func<object, Task> handler, int priority)
    {
        var list = _handlers.GetOrAdd(eventType, _ => new List<PrioritizedHandler>());
        var item = new PrioritizedHandler(handler, priority);

        lock (list)
        {
            if (list.Count == 0)
            {
                list.Add(item);
                return;
            }
            var index = list.BinarySearch(item, PriorityComparer);
            if (index < 0) index = ~index;
            list.Insert(index, item);
        }
    }

    private Func<object, Task> CreateHandlerWrapper(Type type, MethodInfo method, object instance)
    {
        return evt =>
        {
            try
            {
                var result = method.Invoke(instance, new[] { evt });

                if (result is Task t)
                {
                    if (t.Exception != null)
                        _logger?.LogError(t.Exception,
                            "Listener {Type}.{Method} threw an exception", type.Name, method.Name);
                    return t;
                }

                return Task.CompletedTask;
            }
            catch (TargetInvocationException ex)
            {
                _logger?.LogError(ex.InnerException ?? ex,
                    "Error invoking listener {Type}.{Method}", type.Name, method.Name);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Error invoking listener {Type}.{Method}", type.Name, method.Name);
                return Task.CompletedTask;
            }
        };
    }

    public void Publish<TEvent>(TEvent evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        Publish((object)evt);
    }

    public void Publish(object evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        if (!_channel.Writer.TryWrite(evt))
        {
            _logger?.LogWarning("EventBus channel full, failed to publish event {Event}", evt.GetType().Name);
        }
        else
        {
            _logger?.LogDebug("Published event {Event} (fire-and-forget)", evt.GetType().Name);
        }
    }

    public bool TryPublish<TEvent>(TEvent evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        return _channel.Writer.TryWrite(evt!);
    }

    public bool TryPublish(object evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        return _channel.Writer.TryWrite(evt);
    }

    public Task PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        return PublishAsync((object)evt!, cancellationToken);
    }

    public async Task PublishAsync(object evt, CancellationToken cancellationToken = default)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        _logger?.LogDebug("Publishing event {Event} (async)", evt.GetType().Name);
        await _channel.Writer.WriteAsync(evt, cancellationToken).ConfigureAwait(false);
    }

    private List<PrioritizedHandler> GetHandlersSnapshot(Type eventType)
    {
        var result = new List<PrioritizedHandler>();

        if (_handlers.TryGetValue(eventType, out var exact))
        {
            lock (exact) result.AddRange(exact);
        }

        foreach (var iface in eventType.GetInterfaces())
        {
            if (_handlers.TryGetValue(iface, out var list))
            {
                lock (list) result.AddRange(list);
            }
        }

        var baseType = eventType.BaseType;
        while (baseType != null && baseType != typeof(object) && baseType != typeof(ValueType))
        {
            if (_handlers.TryGetValue(baseType, out var list))
            {
                lock (list) result.AddRange(list);
            }
            baseType = baseType.BaseType;
        }

        if (result.Count > 1)
            result.Sort(PriorityComparer);

        return result;
    }

    private async Task ExecuteHandlers(Type eventType, object evt)
    {
        var snapshot = GetHandlersSnapshot(eventType);
        if (snapshot.Count == 0)
        {
            _logger?.LogDebug("No handlers found for event {Event}", eventType.Name);
            return;
        }

        _logger?.LogDebug("Executing {Count} handler(s) for event {Event}",
            snapshot.Count, eventType.Name);

        var tasks = snapshot.Select(h => h.Handler(evt));
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing event {Event}", eventType.Name);
            throw;
        }
    }

    private async Task WorkerLoop(CancellationToken token)
    {
        try
        {
            await foreach (var evt in _channel.Reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                var eventType = evt.GetType();
                var snapshot = GetHandlersSnapshot(eventType);
                if (snapshot.Count > 0)
                {
                    _logger?.LogTrace("Worker processing event {Event} with {Count} handler(s)",
                        eventType.Name, snapshot.Count);

                    var tasks = snapshot.Select(h => h.Handler(evt));
                    try { await Task.WhenAll(tasks).ConfigureAwait(false); }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error in worker processing event {Event}", eventType.Name);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogTrace("WorkerLoop cancelled");
        }
    }

    public async Task StopAsync()
    {
        _logger?.LogInformation("Stopping EventBus with {Count} worker(s)", _workerCount);
        _channel.Writer.TryComplete();
        try
        {
            await Task.WhenAll(_workers).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogTrace("EventBus workers cancelled as expected");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _cts.Cancel();
        _cts.Dispose();
    }
}
