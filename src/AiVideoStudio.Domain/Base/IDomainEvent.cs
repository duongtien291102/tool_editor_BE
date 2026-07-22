using System;

namespace AiVideoStudio.Domain.Base;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
