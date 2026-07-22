using AiVideoStudio.Application.Background;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Background;

public class BackgroundJobService : IBackgroundJobService
{
    public Task EnqueueAsync(string jobName, CancellationToken cancellationToken = default)
    {
        // Integration with Hangfire or RabbitMQ
        Console.WriteLine($"[BackgroundJob] Enqueued job: {jobName}");
        return Task.CompletedTask;
    }

    public Task EnqueueAsync<T>(T args, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[BackgroundJob] Enqueued job of type: {typeof(T).Name}");
        return Task.CompletedTask;
    }
}
