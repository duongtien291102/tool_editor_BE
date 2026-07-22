using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Application.Events;

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IDomainEvent;
}
