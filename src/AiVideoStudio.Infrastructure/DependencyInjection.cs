using AiVideoStudio.Application.Auth;
using AiVideoStudio.Application.Background;
using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Application.Storage;
using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Application.Interfaces.Export;
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
        services.AddScoped<AiVideoStudio.Application.Interfaces.ITransactionManager, AiVideoStudio.Infrastructure.Mongo.MongoTransactionManager>();
        services.AddTransient<IStorageProvider, LocalStorageProvider>();
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

        services.AddTransient<ISeeder, UserSeeder>();
        services.AddTransient<ISeeder, RoleSeeder>();
        services.AddTransient<ISeeder, WorkspaceSeeder>();
        services.AddTransient<ISeeder, SettingSeeder>();
        services.AddTransient<SeedRunner>();

        return services;
    }
}


