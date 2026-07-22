using System.Diagnostics;
using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Application.Interfaces.Render;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Infrastructure.Render;

public abstract class AbstractRenderProvider : IRenderProvider
{
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<ProviderOptions> _options;
    private readonly IApiKeyProvider _apiKeyProvider;

    protected AbstractRenderProvider(
        ILogger logger,
        IOptionsMonitor<ProviderOptions> options,
        IApiKeyProvider apiKeyProvider)
    {
        _logger = logger;
        _options = options;
        _apiKeyProvider = apiKeyProvider;
    }

    public abstract RenderProvider Provider { get; }
    public abstract IReadOnlySet<ProviderCapability> Capabilities { get; }
    public virtual string ProviderName => Provider.ToString();

    protected ProviderOptions Options => _options.Get(Provider.ToString());
    protected string? ApiKey => _apiKeyProvider.GetApiKey(Provider);

    public async Task<RenderResult> RenderAsync(
        RenderJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        var stopwatch = Stopwatch.StartNew();
        var options = Options;

        if (!options.Enabled)
        {
            return RenderResult.Failed(
                "PROVIDER_DISABLED",
                $"Provider '{ProviderName}' is disabled.",
                stopwatch.Elapsed);
        }

        if (TryMapCapability(job.JobType, out var requiredCapability) &&
            !Capabilities.Contains(requiredCapability))
        {
            return RenderResult.Failed(
                "CAPABILITY_NOT_SUPPORTED",
                $"Provider '{ProviderName}' does not support '{requiredCapability}'.",
                stopwatch.Elapsed);
        }

        _logger.LogInformation(
            "Provider {Provider} started render job {JobId}.",
            ProviderName,
            job.Id);

        try
        {
            var payload = await ExecuteWithRetryAsync(
                async attemptToken =>
                {
                    using var timeoutSource = new CancellationTokenSource(
                        TimeSpan.FromSeconds(options.Timeout));
                    using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        timeoutSource.Token);

                    try
                    {
                        return await RenderInternalAsync(job, linkedSource.Token);
                    }
                    catch (OperationCanceledException) when (
                        timeoutSource.IsCancellationRequested &&
                        !cancellationToken.IsCancellationRequested)
                    {
                        throw new TimeoutException(
                            $"Provider '{ProviderName}' exceeded its {options.Timeout}s timeout.");
                    }
                },
                options.Retry,
                cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Provider {Provider} completed render job {JobId} in {ElapsedMs}ms.",
                ProviderName,
                job.Id,
                stopwatch.ElapsedMilliseconds);

            return RenderResult.Succeeded(payload, stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Provider {Provider} cancelled render job {JobId}.",
                ProviderName,
                job.Id);
            return RenderResult.Failed(
                "CANCELLED",
                "Render was cancelled by the user or system.",
                stopwatch.Elapsed);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var mapped = MapException(exception);
            _logger.LogError(
                exception,
                "Provider {Provider} failed render job {JobId} with {ErrorCode}.",
                ProviderName,
                job.Id,
                mapped.ErrorCode);
            return RenderResult.Failed(mapped.ErrorCode, mapped.ErrorMessage, stopwatch.Elapsed);
        }
    }

    public virtual Task CancelAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Provider {Provider} received cancellation for job {JobId}.",
            ProviderName,
            jobId);
        return Task.CompletedTask;
    }

    protected abstract Task<string?> RenderInternalAsync(
        RenderJob job,
        CancellationToken cancellationToken);

    protected virtual (string ErrorCode, string ErrorMessage) MapException(Exception exception) =>
        exception switch
        {
            TimeoutException => ("PROVIDER_TIMEOUT", exception.Message),
            HttpRequestException => ("PROVIDER_CONNECTION_ERROR", exception.Message),
            _ => ("PROVIDER_ERROR", exception.Message)
        };

    protected async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int retryCount,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (attempt < retryCount)
            {
                var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt));
                _logger.LogWarning(
                    exception,
                    "Provider {Provider} attempt {Attempt} failed; retrying in {DelayMs}ms.",
                    ProviderName,
                    attempt + 1,
                    delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static bool TryMapCapability(
        RenderJobType jobType,
        out ProviderCapability capability)
    {
        capability = jobType switch
        {
            RenderJobType.GenerateImage => ProviderCapability.GenerateImage,
            RenderJobType.GenerateVideo => ProviderCapability.GenerateVideo,
            RenderJobType.GenerateVoice => ProviderCapability.GenerateVoice,
            RenderJobType.GenerateSubtitle => ProviderCapability.GenerateSubtitle,
            _ => default
        };

        return jobType != RenderJobType.RenderTimeline;
    }
}
