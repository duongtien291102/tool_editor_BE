using AiVideoStudio.Application.Features.Workflows.DTOs;using AiVideoStudio.Domain.Entities;using AutoMapper;
namespace AiVideoStudio.Application.Features.Workflows.Mappings;
public sealed class WorkflowProfile:Profile{public WorkflowProfile(){CreateMap<WorkflowVariable,WorkflowVariableDto>();CreateMap<WorkflowStep,WorkflowStepDto>();CreateMap<AIWorkflow,WorkflowDto>();CreateMap<WorkflowExecution,WorkflowExecutionDto>();CreateMap<AIWorkflow,WorkflowSummaryDto>().ConstructUsing(x=>new(x.Id,x.ProjectId,x.Name,x.Status,x.IsPaused,x.Steps.Count,x.Version,x.CreatedAt,x.UpdatedAt));}}
