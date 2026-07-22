using System.ComponentModel.DataAnnotations;
namespace AiVideoStudio.Application.Configuration;
public sealed class SystemOptions{public const string SectionName="System";[Required]public string SigningKey{get;set;}=string.Empty;public int RateLimitPermitCount{get;set;}=100;public int RateLimitWindowSeconds{get;set;}=60;}
public sealed class WorkflowOptions{public const string SectionName="Workflow";public int WorkerConcurrency{get;set;}=2;public int DefaultTimeoutSeconds{get;set;}=60;}
public sealed class RenderOptions{public const string SectionName="Render";public int WorkerConcurrency{get;set;}=2;public int RetentionDays{get;set;}=30;}
public sealed class NotificationOptions{public const string SectionName="Notification";public int RetentionDays{get;set;}=90;public int PageSize{get;set;}=20;}
public sealed class MaintenanceOptions{public const string SectionName="Maintenance";public bool Enabled{get;set;}=true;public int IntervalMinutes{get;set;}=1440;public int InitialDelaySeconds{get;set;}=30;public int UploadRetentionDays{get;set;}=7;public int RenderRetentionDays{get;set;}=30;public int WorkflowRetentionDays{get;set;}=30;public int ExportRetentionDays{get;set;}=30;public int NotificationRetentionDays{get;set;}=90;public int AuditRetentionDays{get;set;}=180;public int UsageRetentionDays{get;set;}=180;public int TemporaryFileRetentionHours{get;set;}=24;}
public sealed class HealthOptions{public const string SectionName="Health";public int TimeoutSeconds{get;set;}=3;}
public sealed class MetricsOptions{public const string SectionName="Metrics";public bool Enabled{get;set;}=true;public int MaxSeries{get;set;}=1000;}
