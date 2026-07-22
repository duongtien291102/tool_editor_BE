using AiVideoStudio.Application.Events;
using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Infrastructure.Events;

public class InMemoryEventBus : IEventBus
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        return Task.CompletedTask;
    }
}
