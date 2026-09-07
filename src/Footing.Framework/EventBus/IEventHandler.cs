namespace Footing.Framework.EventBus;

public interface IEventHandler<in TEvent>
{
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}
