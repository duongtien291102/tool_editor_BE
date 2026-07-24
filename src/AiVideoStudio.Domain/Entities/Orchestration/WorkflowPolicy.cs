namespace AiVideoStudio.Domain.Entities.Orchestration;

public sealed class WorkflowPolicy
{
    public int MaxRetry { get; private set; } = 3;
    public bool ContinueOnFailure { get; private set; } = false;
    public int Parallelism { get; private set; } = 4;
    public int BatchSize { get; private set; } = 5;
    public string? ProviderFallback { get; private set; }
    public int TimeoutSeconds { get; private set; } = 300;
    public string Cancellation { get; private set; } = "Cascade";

    private WorkflowPolicy() { }

    public WorkflowPolicy(
        int maxRetry = 3,
        bool continueOnFailure = false,
        int parallelism = 4,
        int batchSize = 5,
        string? providerFallback = null,
        int timeoutSeconds = 300,
        string cancellation = "Cascade")
    {
        if (maxRetry < 0) throw new ArgumentOutOfRangeException(nameof(maxRetry));
        if (parallelism <= 0) throw new ArgumentOutOfRangeException(nameof(parallelism));
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (timeoutSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));

        MaxRetry = maxRetry;
        ContinueOnFailure = continueOnFailure;
        Parallelism = parallelism;
        BatchSize = batchSize;
        ProviderFallback = providerFallback;
        TimeoutSeconds = timeoutSeconds;
        Cancellation = cancellation;
    }
}
