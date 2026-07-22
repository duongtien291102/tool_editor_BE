using AiVideoStudio.Domain.Entities;using AiVideoStudio.Domain.Enums;
namespace AiVideoStudio.Application.Interfaces.Operations;
public sealed record HealthComponent(string Name,bool Healthy,string? Detail=null,TimeSpan? Duration=null);public sealed record SystemHealth(bool Healthy,DateTimeOffset CheckedAt,IReadOnlyList<HealthComponent> Components);
public interface IHealthCheckService{Task<SystemHealth> CheckAsync(bool readiness,CancellationToken ct=default);}
public interface IMetricsCollector{void Increment(string name,long value=1);void Observe(string name,TimeSpan duration);void SetGauge(string name,long value);IReadOnlyDictionary<string,double> Snapshot();}
public interface IAuditWriter{Task WriteAsync(string user,AuditActionType action,string result,string resource,CancellationToken ct=default);}
public interface IUsageTracker{Task RecordAsync(string user,QuotaType type,long amount,string resource,CancellationToken ct=default);}
public interface INotificationDispatcher{Task<SystemNotification> DispatchAsync(string user,string title,string message,NotificationType type,CancellationToken ct=default);}
public interface IMaintenanceRunner{Task<MaintenanceTask> RunAsync(string name,string requestedBy,CancellationToken ct=default);}
public interface IRateLimiter{bool TryAcquire(string key);}
public interface ISignedUrlService{Uri Sign(Uri resource,TimeSpan lifetime);bool Validate(Uri signedUrl);}
public interface ICorrelationIdProvider{string CorrelationId{get;}}
public interface IRequestContext{string RequestId{get;}string CorrelationId{get;}string? UserId{get;}string? IpAddress{get;}}
