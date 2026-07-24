using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Features.Orchestration.DTOs;

public sealed record GenerationWorkflowDto(
    string Id,
    string ProjectId,
    string? SceneId,
    string? ShotId,
    string OwnerId,
    string Name,
    string? Description,
    WorkflowState State,
    WorkflowPolicyDto Policy,
    Dictionary<string, string> Context,
    List<OrchestrationStepDto> Steps,
    int Version,
    string? CurrentExecutionId,
    string CorrelationId,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record OrchestrationStepDto(
    string Id,
    string Name,
    WorkflowStepType Type,
    WorkflowStepStatus Status,
    string? ParentId,
    List<string> Children,
    List<string> DependsOn,
    string? ParallelGroupId,
    string? SequentialGroupId,
    string? Condition,
    string? Provider,
    string? Resolution,
    string? Style,
    string? AspectRatio,
    string? Model,
    string? RenderJobId,
    int TimeoutSeconds,
    int MaxRetries,
    int RetryCount,
    bool IsCompensated,
    string? CompensationAction,
    Dictionary<string, string> InputContext,
    Dictionary<string, string> OutputContext,
    string? Error);

public sealed record WorkflowPolicyDto(
    int MaxRetry,
    bool ContinueOnFailure,
    int Parallelism,
    int BatchSize,
    string? ProviderFallback,
    int TimeoutSeconds,
    string Cancellation);

public sealed record WorkflowHistoryDto(
    string Id,
    string WorkflowId,
    string? StepId,
    WorkflowState State,
    string Message,
    string? Details,
    string CorrelationId,
    DateTimeOffset Timestamp);

public sealed record CreateGenerationWorkflowRequest(
    string ProjectId,
    string Name,
    string? Description,
    List<CreateOrchestrationStepDto> Steps,
    WorkflowPolicyDto? Policy = null,
    Dictionary<string, string>? Context = null,
    string? SceneId = null,
    string? ShotId = null);

public sealed record CreateOrchestrationStepDto(
    string Name,
    WorkflowStepType Type,
    List<string>? DependsOn = null,
    string? ParentId = null,
    List<string>? Children = null,
    string? ParallelGroupId = null,
    string? SequentialGroupId = null,
    string? Condition = null,
    string? Provider = null,
    string? Resolution = null,
    string? Style = null,
    string? AspectRatio = null,
    string? Model = null,
    int TimeoutSeconds = 60,
    int MaxRetries = 3,
    Dictionary<string, string>? Inputs = null,
    string? Id = null);
