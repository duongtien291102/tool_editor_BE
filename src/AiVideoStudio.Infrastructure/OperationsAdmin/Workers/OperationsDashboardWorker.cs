using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiVideoStudio.Infrastructure.OperationsAdmin.Workers;

public sealed class OperationsDashboardWorker : BackgroundService
{
    private readonly ILogger<OperationsDashboardWorker> _logger;

    public OperationsDashboardWorker(ILogger<OperationsDashboardWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OperationsDashboardWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Perform periodic aggregation of operations telemetry and metrics
                _logger.LogDebug("OperationsDashboardWorker collecting system telemetry snapshot.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in OperationsDashboardWorker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
