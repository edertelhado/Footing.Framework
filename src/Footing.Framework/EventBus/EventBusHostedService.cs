using Microsoft.Extensions.Hosting;
namespace Footing.Framework.EventBus;

public class EventBusHostedService : IHostedService
{
    private readonly EventBus _eventBus;

    public EventBusHostedService(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _eventBus.StopAsync();
    }
}
