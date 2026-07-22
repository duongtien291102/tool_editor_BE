using System.Text.Json;
using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Features.Exports.Models;
using AiVideoStudio.Application.Interfaces.Export;
using AiVideoStudio.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Infrastructure.Export;

public sealed class MockExportProvider : IExportProvider
{
    private readonly ExportOptions _options;
    private readonly ILogger<MockExportProvider> _logger;

    public MockExportProvider(IOptions<ExportOptions> options, ILogger<MockExportProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExportProviderResult> ExportAsync(
        FFmpegCommandModel command,
        Func<ExportProgressUpdate, CancellationToken, Task> progress,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= _options.RetryCount; attempt++)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            try
            {
                return await ExecuteMockAsync(command, progress, linked.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                if (attempt == _options.RetryCount)
                    return ExportProviderResult.Failure("EXPORT_TIMEOUT", "Mock export exceeded its configured timeout.");
                _logger.LogWarning("Mock export timed out on attempt {Attempt}; retrying.", attempt + 1);
            }
            catch (Exception exception)
            {
                if (attempt == _options.RetryCount)
                    return ExportProviderResult.Failure("EXPORT_PROVIDER_ERROR", exception.Message);
                _logger.LogWarning(exception, "Mock export failed on attempt {Attempt}; retrying.", attempt + 1);
            }
        }

        return ExportProviderResult.Failure("EXPORT_PROVIDER_ERROR", "Mock export failed unexpectedly.");
    }

    private async Task<ExportProviderResult> ExecuteMockAsync(
        FFmpegCommandModel command,
        Func<ExportProgressUpdate, CancellationToken, Task> progress,
        CancellationToken cancellationToken)
    {
        await ReportAsync(ExportStatus.Preparing, 10, progress, cancellationToken);
        await ReportAsync(ExportStatus.Preparing, 25, progress, cancellationToken);
        await ReportAsync(ExportStatus.Rendering, 45, progress, cancellationToken);
        await ReportAsync(ExportStatus.Rendering, 70, progress, cancellationToken);
        await ReportAsync(ExportStatus.Muxing, 90, progress, cancellationToken);
        await ReportAsync(ExportStatus.Muxing, 99, progress, cancellationToken);

        Directory.CreateDirectory(command.OutputOptions.OutputDirectory);
        var outputPath = Path.Combine(command.OutputOptions.OutputDirectory, command.OutputOptions.FileName);
        var manifest = JsonSerializer.Serialize(command, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, manifest, cancellationToken);
        _logger.LogInformation("Mock export manifest created at {OutputPath}.", outputPath);
        return ExportProviderResult.Success(outputPath);
    }

    private async Task ReportAsync(
        ExportStatus status,
        int value,
        Func<ExportProgressUpdate, CancellationToken, Task> progress,
        CancellationToken cancellationToken)
    {
        await Task.Delay(_options.MockStepDelayMilliseconds, cancellationToken);
        await progress(new ExportProgressUpdate(status, value), cancellationToken);
    }
}
