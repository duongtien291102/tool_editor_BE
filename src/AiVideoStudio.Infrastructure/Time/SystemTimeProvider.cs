using AiVideoStudio.Shared.Interfaces;
using System;

namespace AiVideoStudio.Infrastructure.Time;

public class SystemTimeProvider : IAppTimeProvider
{
    private readonly TimeProvider _timeProvider;

    public SystemTimeProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();
}
