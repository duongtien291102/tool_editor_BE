using AiVideoStudio.Shared.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace AiVideoStudio.Infrastructure.Logging;

public class SerilogLogger<T> : IAppLogger<T>
{
    private readonly ILogger<T> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    private readonly string _machineName;
    private readonly string _environment;
    private readonly string _applicationVersion;

    public SerilogLogger(ILogger<T> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        _machineName = Environment.MachineName;
        _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        _applicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
    }

    private string? GetTraceId()
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (string.IsNullOrEmpty(traceId) || traceId == "00000000000000000000000000000000")
        {
            // Fallback to CorrelationId
            var correlationId = _httpContextAccessor.HttpContext?.Items["X-Correlation-Id"]?.ToString();
            return correlationId;
        }
        return traceId;
    }

    private object EnrichData(object? data)
    {
        var traceId = GetTraceId();
        
        return new 
        {
            MachineName = _machineName,
            Environment = _environment,
            ApplicationVersion = _applicationVersion,
            TraceId = traceId,
            Payload = data
        };
    }

    public void LogInformation(int eventId, string message, object? data = null)
    {
        _logger.LogInformation(new EventId(eventId), "{Message} {@Data}", message, EnrichData(data));
    }

    public void LogWarning(int eventId, string message, object? data = null)
    {
        _logger.LogWarning(new EventId(eventId), "{Message} {@Data}", message, EnrichData(data));
    }

    public void LogError(int eventId, Exception? exception, string message, object? data = null)
    {
        _logger.LogError(new EventId(eventId), exception, "{Message} {@Data}", message, EnrichData(data));
    }

    public void LogDebug(int eventId, string message, object? data = null)
    {
        _logger.LogDebug(new EventId(eventId), "{Message} {@Data}", message, EnrichData(data));
    }

    public void LogCritical(int eventId, Exception? exception, string message, object? data = null)
    {
        _logger.LogCritical(new EventId(eventId), exception, "{Message} {@Data}", message, EnrichData(data));
    }
}
