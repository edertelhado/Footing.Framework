using Microsoft.Extensions.Hosting;
namespace Footing.Framework.EventBus;

public class EventBusHostedService : IHostedService
{
    private readonly EventBus _eventBus;
    private readonly IServiceProvider _serviceProvider;

    public EventBusHostedService(EventBus eventBus, IServiceProvider serviceProvider)
    {
        _eventBus = eventBus;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _eventBus.RegisterListenersFromProvider(_serviceProvider);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _eventBus.StopAsync();
    }
}
