using System;

namespace AiVideoStudio.Shared.Interfaces;

public interface IAppTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
