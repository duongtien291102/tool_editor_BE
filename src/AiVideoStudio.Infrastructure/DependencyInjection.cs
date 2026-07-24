using AiVideoStudio.Application.Auth;
using AiVideoStudio.Application.Background;
using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Application.Storage;
using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Application.Interfaces.Workflow;
using AiVideoStudio.Application.Interfaces.Operations;
using AiVideoStudio.Infrastructure.Auth;
using AiVideoStudio.Infrastructure.Background;
using AiVideoStudio.Infrastructure.Configuration;
using AiVideoStudio.Infrastructure.Data.Seed;
using AiVideoStudio.Infrastructure.Data.Seed.Seeders;
using AiVideoStudio.Infrastructure.Events;
using AiVideoStudio.Infrastructure.IdGeneration;
using AiVideoStudio.Infrastructure.Logging;
using AiVideoStudio.Infrastructure.Mongo;
using AiVideoStudio.Infrastructure.Redis;
using AiVideoStudio.Infrastructure.Storage;
using AiVideoStudio.Infrastructure.Time;
using AiVideoStudio.Shared.Configuration;
using AiVideoStudio.Shared.Interfaces;
using AiVideoStudio.Shared.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;

namespace AiVideoStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IAppConfiguration, AppConfiguration>();
        services.AddSingleton(typeof(IAppLogger<>), typeof(SerilogLogger<>));
        
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAppTimeProvider, SystemTimeProvider>();
        services.AddSingleton<IIdGenerator, GuidIdGenerator>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        EnvLoader.Load();

        services.AddSingleton<MongoDB.Driver.IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoOptions>>().Value;
            var settings = MongoDB.Driver.MongoClientSettings.FromConnectionString(options.ConnectionString);
            settings.SslSettings = new MongoDB.Driver.SslSettings
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            };
            return new MongoDB.Driver.MongoClient(settings);
        });
        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IUserRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.UserRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IProjectRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.ProjectRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IMediaAssetRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.MediaAssetRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IScriptRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.ScriptRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.ITimelineRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.TimelineRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IRefreshTokenRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.RefreshTokenRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IRoleRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.RoleRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IPermissionRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.PermissionRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IUserTokenRepository, AiVideoStudio.Infrastructure.Persistence.Repositories.UserTokenRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IPasswordHistoryRepository, AiVideoStudio.Infrastructure.Persistence.Repositories.PasswordHistoryRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IEmailOutboxRepository, AiVideoStudio.Infrastructure.Persistence.Repositories.EmailOutboxRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IAuditLogRepository, AiVideoStudio.Infrastructure.Persistence.Repositories.AuditLogRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IRenderJobRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.RenderJobRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IExportJobRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.ExportJobRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IUploadSessionRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.UploadSessionRepository>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.ITransactionManager, AiVideoStudio.Infrastructure.Mongo.MongoTransactionManager>();
        services.AddSingleton<IStorageProvider, MockStorageProvider>();
        services.AddScoped<IChunkUploadEngine, ChunkUploadEngine>();
        services.AddScoped<IThumbnailGenerator, MockThumbnailGenerator>();
        services.AddScoped<IMetadataExtractor, MockMetadataExtractor>();
        services.AddScoped<IAssetManifestBuilder, AssetManifestBuilder>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddSingleton<IBackgroundJobService, BackgroundJobService>();

        // Render Pipeline and extensible provider framework
        services.AddSingleton<IRenderQueue, AiVideoStudio.Infrastructure.Render.InMemoryRenderQueue>();

        foreach (var provider in Enum.GetValues<AiVideoStudio.Domain.Enums.RenderProvider>())
        {
            services.AddOptions<ProviderOptions>(provider.ToString())
                .Bind(configuration.GetSection($"{ProviderOptions.SectionName}:{provider}"))
                .ValidateDataAnnotations();
        }

        services.AddSingleton<IApiKeyProvider, AiVideoStudio.Infrastructure.Render.MemoryApiKeyProvider>();
        services.AddSingleton<IRenderProvider, AiVideoStudio.Infrastructure.Render.MockRenderProvider>();
        services.AddSingleton<IRenderProvider, AiVideoStudio.Infrastructure.Render.MockOpenAIProvider>();
        services.AddSingleton<IRenderProvider, AiVideoStudio.Infrastructure.Render.MockRunwayProvider>();
        services.AddSingleton<IRenderProvider, AiVideoStudio.Infrastructure.Render.MockKlingProvider>();
        services.AddSingleton<IRenderProvider, AiVideoStudio.Infrastructure.Render.MockVeoProvider>();
        services.AddSingleton<IRenderProvider, AiVideoStudio.Infrastructure.Render.MockElevenLabsProvider>();
        services.AddSingleton<IRenderProvider, AiVideoStudio.Infrastructure.Render.MockStableVideoProvider>();
        services.AddSingleton<IRenderProviderRegistry, AiVideoStudio.Infrastructure.Render.RenderProviderRegistry>();
        services.AddSingleton<AiVideoStudio.Infrastructure.Render.MockProviderHealthChecker>();
        services.AddSingleton<IProviderHealthChecker>(provider =>
            provider.GetRequiredService<AiVideoStudio.Infrastructure.Render.MockProviderHealthChecker>());
        services.AddSingleton<IProviderSelector, AiVideoStudio.Infrastructure.Render.FirstAvailableProviderSelector>();
        services.AddSingleton<IRenderProviderFactory, AiVideoStudio.Infrastructure.Render.RenderProviderFactory>();
        services.AddSingleton<AiVideoStudio.Infrastructure.Render.RenderWorker>();
        services.AddSingleton<IRenderJobCanceller>(provider => provider.GetRequiredService<AiVideoStudio.Infrastructure.Render.RenderWorker>());
        services.AddHostedService(provider => provider.GetRequiredService<AiVideoStudio.Infrastructure.Render.RenderWorker>());

        // Export Engine (independent from Render Queue and AI provider framework)
        services.AddOptions<ExportOptions>()
            .Bind(configuration.GetSection(ExportOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IExportQueue, AiVideoStudio.Infrastructure.Export.InMemoryExportQueue>();
        services.AddScoped<ITimelineResolver, AiVideoStudio.Infrastructure.Export.TimelineResolver>();
        services.AddScoped<ITrackResolver, AiVideoStudio.Infrastructure.Export.TrackResolver>();
        services.AddScoped<IClipResolver, AiVideoStudio.Infrastructure.Export.ClipResolver>();
        services.AddScoped<IAssetResolver, AiVideoStudio.Infrastructure.Export.AssetResolver>();
        services.AddSingleton<IExportGraphBuilder, AiVideoStudio.Infrastructure.Export.ExportGraphBuilder>();
        services.AddSingleton<IFFmpegCommandBuilder, AiVideoStudio.Infrastructure.Export.FFmpegCommandBuilder>();
        services.AddSingleton<IExportProvider, AiVideoStudio.Infrastructure.Export.MockExportProvider>();
        services.AddScoped<IExportPipeline, AiVideoStudio.Infrastructure.Export.ExportPipeline>();
        services.AddSingleton<AiVideoStudio.Infrastructure.Export.ExportWorker>();
        services.AddSingleton<IExportJobCanceller>(provider =>
            provider.GetRequiredService<AiVideoStudio.Infrastructure.Export.ExportWorker>());
        services.AddHostedService(provider => provider.GetRequiredService<AiVideoStudio.Infrastructure.Export.ExportWorker>());

        // AI Workflow orchestration (capability-based; providers remain behind the existing factory)
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IAIWorkflowRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.AIWorkflowRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IWorkflowExecutionRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.WorkflowExecutionRepository>();
        services.AddSingleton<AiVideoStudio.Domain.Interfaces.IWorkflowResolver, AiVideoStudio.Infrastructure.Workflow.WorkflowResolver>();
        services.AddSingleton<AiVideoStudio.Domain.Interfaces.IWorkflowScheduler, AiVideoStudio.Infrastructure.Workflow.InMemoryWorkflowScheduler>();
        services.AddSingleton<IWorkflowStepDispatcher, AiVideoStudio.Infrastructure.Workflow.WorkflowStepDispatcher>();
        services.AddSingleton<AiVideoStudio.Domain.Interfaces.IWorkflowExecutor, AiVideoStudio.Infrastructure.Workflow.WorkflowExecutor>();
        services.AddSingleton<AiVideoStudio.Infrastructure.Workflow.WorkflowWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<AiVideoStudio.Infrastructure.Workflow.WorkflowWorker>());
        services.AddHostedService<AiVideoStudio.Infrastructure.Workflow.WorkflowIndexInitializer>();

        // Task 5.5 AI Generation Orchestration Engine
        services.AddScoped<AiVideoStudio.Domain.Interfaces.Orchestration.IGenerationWorkflowRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.GenerationWorkflowRepository>();
        services.AddSingleton<AiVideoStudio.Application.Features.Orchestration.Services.IWorkflowSchedulerEngine, AiVideoStudio.Application.Features.Orchestration.Services.WorkflowSchedulerEngine>();
        services.AddSingleton<AiVideoStudio.Application.Features.Orchestration.Services.IOrchestrationDispatcher, AiVideoStudio.Application.Features.Orchestration.Services.OrchestrationDispatcher>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.Orchestration.IGenerationOrchestrator, AiVideoStudio.Application.Features.Orchestration.Services.GenerationOrchestrator>();
        services.AddSingleton<AiVideoStudio.Infrastructure.Orchestration.OrchestrationMetrics>();
        services.AddSingleton<AiVideoStudio.Infrastructure.Orchestration.GenerationWorkflowWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<AiVideoStudio.Infrastructure.Orchestration.GenerationWorkflowWorker>());
        services.AddScoped<AiVideoStudio.Infrastructure.Orchestration.WorkflowOrchestratorHealthCheck>();

        // Production operations foundation
        services.AddOptions<SystemOptions>().Bind(configuration.GetSection(SystemOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<WorkflowOptions>().Bind(configuration.GetSection(WorkflowOptions.SectionName));
        services.AddOptions<RenderOptions>().Bind(configuration.GetSection(RenderOptions.SectionName));
        services.AddOptions<NotificationOptions>().Bind(configuration.GetSection(NotificationOptions.SectionName));
        services.AddOptions<MaintenanceOptions>().Bind(configuration.GetSection(MaintenanceOptions.SectionName));
        services.AddOptions<HealthOptions>().Bind(configuration.GetSection(HealthOptions.SectionName));
        services.AddOptions<MetricsOptions>().Bind(configuration.GetSection(MetricsOptions.SectionName));
        services.AddScoped<AiVideoStudio.Domain.Interfaces.INotificationRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.NotificationRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IQuotaRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.QuotaRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.ISystemConfigurationRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.SystemConfigurationRepository>();
        services.AddScoped<AiVideoStudio.Domain.Interfaces.IMaintenanceRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.MaintenanceRepository>();
        services.AddSingleton<IMetricsCollector, AiVideoStudio.Infrastructure.Operations.MetricsCollector>();
        services.AddScoped<IAuditWriter, AiVideoStudio.Infrastructure.Operations.AuditWriter>();
        services.AddScoped<IUsageTracker, AiVideoStudio.Infrastructure.Operations.UsageTracker>();
        services.AddScoped<INotificationDispatcher, AiVideoStudio.Infrastructure.Operations.NotificationDispatcher>();
        services.AddScoped<IMaintenanceRunner, AiVideoStudio.Infrastructure.Operations.MaintenanceRunner>();
        services.AddSingleton<IHealthCheckService, AiVideoStudio.Infrastructure.Operations.ProductionHealthCheckService>();
        services.AddSingleton<IRateLimiter, AiVideoStudio.Infrastructure.Operations.FixedWindowRateLimiter>();
        services.AddSingleton<ISignedUrlService, AiVideoStudio.Infrastructure.Operations.SignedUrlService>();
        services.AddScoped<AiVideoStudio.Infrastructure.Operations.RequestContext>();
        services.AddScoped<IRequestContext>(p => p.GetRequiredService<AiVideoStudio.Infrastructure.Operations.RequestContext>());
        services.AddScoped<ICorrelationIdProvider>(p => p.GetRequiredService<AiVideoStudio.Infrastructure.Operations.RequestContext>());
        services.AddHostedService<AiVideoStudio.Infrastructure.Operations.MaintenanceWorker>();

        // Task 5.12 Platform Administration, Observability & Operations Center
        services.AddScoped<AiVideoStudio.Domain.Interfaces.OperationsAdmin.IPlatformAdministrationRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.PlatformAdministrationRepository>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.OperationsAdmin.IFeatureFlagService, AiVideoStudio.Application.Features.OperationsAdmin.Services.FeatureFlagService>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.OperationsAdmin.IAuditService, AiVideoStudio.Application.Features.OperationsAdmin.Services.AuditService>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.OperationsAdmin.IIncidentManager, AiVideoStudio.Application.Features.OperationsAdmin.Services.IncidentManager>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.OperationsAdmin.IBackupService, AiVideoStudio.Application.Features.OperationsAdmin.Services.BackupService>();
        services.AddSingleton<AiVideoStudio.Application.Interfaces.OperationsAdmin.ILogExplorerService, AiVideoStudio.Application.Features.OperationsAdmin.Services.LogExplorerService>();
        services.AddSingleton<AiVideoStudio.Application.Interfaces.OperationsAdmin.IMetricsExplorerService, AiVideoStudio.Application.Features.OperationsAdmin.Services.MetricsExplorerService>();
        services.AddSingleton<AiVideoStudio.Application.Interfaces.OperationsAdmin.ITraceExplorerService, AiVideoStudio.Application.Features.OperationsAdmin.Services.TraceExplorerService>();
        services.AddSingleton<AiVideoStudio.Application.Interfaces.OperationsAdmin.IPlatformHealthService, AiVideoStudio.Application.Features.OperationsAdmin.Services.PlatformHealthService>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.OperationsAdmin.IPlatformAdministrationService, AiVideoStudio.Application.Features.OperationsAdmin.Services.PlatformAdministrationService>();
        services.AddSingleton<AiVideoStudio.Infrastructure.OperationsAdmin.Telemetry.PlatformTelemetry>();
        services.AddSingleton<AiVideoStudio.Infrastructure.OperationsAdmin.Health.PlatformHealthCheck>();
        services.AddSingleton<AiVideoStudio.Infrastructure.OperationsAdmin.Workers.RestoreWorker>();
        services.AddSingleton<AiVideoStudio.Infrastructure.OperationsAdmin.Workers.JobReplayWorker>();
        services.AddHostedService<AiVideoStudio.Infrastructure.OperationsAdmin.Workers.OperationsDashboardWorker>();
        services.AddHostedService<AiVideoStudio.Infrastructure.OperationsAdmin.Workers.BackupWorker>();
        services.AddHostedService<AiVideoStudio.Infrastructure.OperationsAdmin.Workers.IncidentWorker>();
        services.AddHostedService<AiVideoStudio.Infrastructure.OperationsAdmin.Workers.MaintenanceWorkerService>();

        // Task 5.13 Platform Security, Governance, Compliance & Zero Trust Engine
        services.AddScoped<AiVideoStudio.Domain.Interfaces.SecurityGovernance.ISecurityRepository, AiVideoStudio.Infrastructure.Mongo.Repositories.SecurityRepository>();
        services.AddSingleton<AiVideoStudio.Application.Interfaces.SecurityGovernance.ISecretsManager, AiVideoStudio.Application.Features.SecurityGovernance.Services.SecretsManager>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.SecurityGovernance.IPolicyEngine, AiVideoStudio.Application.Features.SecurityGovernance.Services.PolicyEngine>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.SecurityGovernance.IRiskEngine, AiVideoStudio.Application.Features.SecurityGovernance.Services.RiskEngine>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.SecurityGovernance.IThreatDetectionService, AiVideoStudio.Application.Features.SecurityGovernance.Services.ThreatDetectionService>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.SecurityGovernance.IComplianceService, AiVideoStudio.Application.Features.SecurityGovernance.Services.ComplianceService>();
        services.AddScoped<AiVideoStudio.Application.Interfaces.SecurityGovernance.ISecurityService, AiVideoStudio.Application.Features.SecurityGovernance.Services.SecurityService>();
        services.AddSingleton<AiVideoStudio.Infrastructure.SecurityGovernance.Telemetry.SecurityTelemetry>();
        services.AddSingleton<AiVideoStudio.Infrastructure.SecurityGovernance.Health.SecurityHealthCheck>();
        services.AddHostedService<AiVideoStudio.Infrastructure.SecurityGovernance.Workers.ThreatDetectionWorker>();
        services.AddHostedService<AiVideoStudio.Infrastructure.SecurityGovernance.Workers.RiskCalculationWorker>();
        services.AddHostedService<AiVideoStudio.Infrastructure.SecurityGovernance.Workers.ComplianceWorker>();
        services.AddHostedService<AiVideoStudio.Infrastructure.SecurityGovernance.Workers.SecretRotationWorker>();

        services.AddTransient<ISeeder, UserSeeder>();
        services.AddTransient<ISeeder, RoleSeeder>();
        services.AddTransient<ISeeder, WorkspaceSeeder>();
        services.AddTransient<ISeeder, SettingSeeder>();
        services.AddTransient<SeedRunner>();

        return services;
    }
}
