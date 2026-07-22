using System;

namespace AiVideoStudio.Shared.Logging;

public interface IAppLogger<T>
{
    void LogInformation(int eventId, string message, object? data = null);
    void LogWarning(int eventId, string message, object? data = null);
    void LogError(int eventId, Exception? exception, string message, object? data = null);
    void LogDebug(int eventId, string message, object? data = null);
    void LogCritical(int eventId, Exception? exception, string message, object? data = null);
}
