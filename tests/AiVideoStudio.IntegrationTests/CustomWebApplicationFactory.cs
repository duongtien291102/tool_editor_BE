using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using AiVideoStudio.Infrastructure.Workflow;
using AiVideoStudio.Application.Interfaces.Operations;
using NSubstitute;

namespace AiVideoStudio.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IMediaAssetRepository MediaAssetRepository { get; } = Substitute.For<IMediaAssetRepository>();
    public IProjectRepository ProjectRepository { get; } = Substitute.For<IProjectRepository>();
    public ITimelineRepository TimelineRepository { get; } = Substitute.For<ITimelineRepository>();
    public IUserRepository UserRepository { get; } = Substitute.For<IUserRepository>();
    public IUserTokenRepository UserTokenRepository { get; } = Substitute.For<IUserTokenRepository>();
    public IPasswordHistoryRepository PasswordHistoryRepository { get; } = Substitute.For<IPasswordHistoryRepository>();
    public IEmailOutboxRepository EmailOutboxRepository { get; } = Substitute.For<IEmailOutboxRepository>();
    public IAuditLogRepository AuditLogRepository { get; } = Substitute.For<IAuditLogRepository>();
    public IRefreshTokenRepository RefreshTokenRepository { get; } = Substitute.For<IRefreshTokenRepository>();
    public IRenderJobRepository RenderJobRepository { get; } = Substitute.For<IRenderJobRepository>();
    public IExportJobRepository ExportJobRepository { get; } = Substitute.For<IExportJobRepository>();
    public IUploadSessionRepository UploadSessionRepository { get; } = Substitute.For<IUploadSessionRepository>();
    public IAIWorkflowRepository AIWorkflowRepository { get; } = Substitute.For<IAIWorkflowRepository>();
    public IWorkflowExecutionRepository WorkflowExecutionRepository { get; } = Substitute.For<IWorkflowExecutionRepository>();
    public INotificationRepository NotificationRepository { get; } = Substitute.For<INotificationRepository>();
    public IQuotaRepository QuotaRepository { get; } = Substitute.For<IQuotaRepository>();
    public ISystemConfigurationRepository SystemConfigurationRepository { get; } = Substitute.For<ISystemConfigurationRepository>();
    public IMaintenanceRepository MaintenanceRepository { get; } = Substitute.For<IMaintenanceRepository>();
    public IHealthCheckService HealthCheckService { get; } = Substitute.For<IHealthCheckService>();
    public IMaintenanceRunner MaintenanceRunner { get; } = Substitute.For<IMaintenanceRunner>();
    public ITransactionManager TransactionManager { get; } = Substitute.For<ITransactionManager>();
    public ICurrentUser CurrentUser { get; } = Substitute.For<ICurrentUser>();

    public IPasswordHasher PasswordHasher { get; } = Substitute.For<IPasswordHasher>();
    public IJwtTokenGenerator JwtGenerator { get; } = Substitute.For<IJwtTokenGenerator>();
    public IRefreshTokenService RefreshTokenService { get; } = Substitute.For<IRefreshTokenService>();
    public IAuthenticationService AuthenticationService { get; } = Substitute.For<IAuthenticationService>();
    public IPermissionResolver PermissionResolver { get; } = Substitute.For<IPermissionResolver>();
    public IEventBus EventBus { get; } = Substitute.For<IEventBus>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                { "Jwt:Key", "SuperSecretJwtKeyForIntegrationTesting1234567890!" },
                { "Jwt:RefreshTokenSecret", "SuperSecretRefreshTokenKeyForTesting123456!" },
                { "Jwt:Issuer", "AiVideoStudioTest" },
                { "Jwt:Audience", "AiVideoStudioTestClient" },
                { "Jwt:AccessTokenLifetimeMinutes", "60" },
                { "Jwt:RefreshTokenLifetimeDays", "7" },
                { "MongoDb:ConnectionString", "mongodb://localhost:27017" },
                { "MongoDb:DatabaseName", "AiVideoStudioTestDb" },
                { "Redis:ConnectionString", "localhost:6379" },
                { "Storage:Provider", "Local" },
                { "Storage:BasePath", "./test_storage" },
                { "Export:OutputDirectory", "./test_exports" },
                { "Export:TimeoutSeconds", "5" },
                { "Export:RetryCount", "0" },
                { "Export:MockStepDelayMilliseconds", "1" },
                { "CorsOrigins:0", "http://localhost:3000" }
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.Replace(ServiceDescriptor.Scoped(_ => MediaAssetRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => ProjectRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => TimelineRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => UserRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => UserTokenRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => PasswordHistoryRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => EmailOutboxRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => AuditLogRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => RefreshTokenRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => RenderJobRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => ExportJobRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => UploadSessionRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => AIWorkflowRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => WorkflowExecutionRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => NotificationRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => QuotaRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => SystemConfigurationRepository));
            services.Replace(ServiceDescriptor.Scoped(_ => MaintenanceRepository));
            services.Replace(ServiceDescriptor.Singleton(_ => HealthCheckService));
            services.Replace(ServiceDescriptor.Scoped(_ => MaintenanceRunner));
            services.Replace(ServiceDescriptor.Scoped(_ => TransactionManager));
            services.Replace(ServiceDescriptor.Scoped(_ => CurrentUser));

            services.Replace(ServiceDescriptor.Scoped(_ => PasswordHasher));
            services.Replace(ServiceDescriptor.Scoped(_ => JwtGenerator));
            services.Replace(ServiceDescriptor.Scoped(_ => RefreshTokenService));
            services.Replace(ServiceDescriptor.Scoped(_ => AuthenticationService));
            services.Replace(ServiceDescriptor.Scoped(_ => PermissionResolver));
            services.Replace(ServiceDescriptor.Scoped(_ => EventBus));

            var workflowIndexInitializer = services.FirstOrDefault(x =>
                x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(WorkflowIndexInitializer));
            if (workflowIndexInitializer is not null) services.Remove(workflowIndexInitializer);
        });
    }
}
