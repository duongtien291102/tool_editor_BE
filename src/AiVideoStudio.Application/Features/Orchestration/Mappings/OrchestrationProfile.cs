using AiVideoStudio.Application.Features.Orchestration.DTOs;
using AiVideoStudio.Domain.Entities.Orchestration;
using AutoMapper;

namespace AiVideoStudio.Application.Features.Orchestration.Mappings;

public sealed class OrchestrationProfile : Profile
{
    public OrchestrationProfile()
    {
        CreateMap<GenerationWorkflow, GenerationWorkflowDto>()
            .ForMember(d => d.Context, opt => opt.MapFrom(s => s.Context.Data));

        CreateMap<OrchestrationStep, OrchestrationStepDto>();
        CreateMap<WorkflowPolicy, WorkflowPolicyDto>();
        CreateMap<WorkflowHistory, WorkflowHistoryDto>();
    }
}
