using AiVideoStudio.Domain.Enums;
namespace AiVideoStudio.Application.Features.Workflows.DTOs;
public record WorkflowVariableDto(string Name,string Value);
public record WorkflowStepDto(string Id,string Name,WorkflowStepType Type,WorkflowStepStatus Status,IReadOnlyList<string> Dependencies,string? Condition,int TimeoutSeconds,int MaxRetries,int RetryCount,IReadOnlyDictionary<string,string> InputContext,IReadOnlyDictionary<string,string> OutputContext,string? Error);
public record WorkflowDto(string Id,string ProjectId,string OwnerId,string Name,string? Description,WorkflowStatus Status,bool IsPaused,int Version,string? CurrentExecutionId,string? Error,IReadOnlyCollection<WorkflowStepDto> Steps,IReadOnlyCollection<WorkflowVariableDto> Variables,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
public record WorkflowSummaryDto(string Id,string ProjectId,string Name,WorkflowStatus Status,bool IsPaused,int StepCount,int Version,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
public record WorkflowExecutionDto(string Id,string WorkflowId,WorkflowStatus Status,DateTimeOffset StartedAt,DateTimeOffset? CompletedAt,IReadOnlyDictionary<string,string> OutputContext,string? Error);
public record WorkflowTemplateDto(string Id,string Name,IReadOnlyCollection<WorkflowStepDto> Steps);
public record WorkflowStepDefinition(string? Id,string Name,WorkflowStepType Type,IReadOnlyList<string> Dependencies,string? Condition,int TimeoutSeconds,int MaxRetries,IReadOnlyDictionary<string,string>? InputContext);
