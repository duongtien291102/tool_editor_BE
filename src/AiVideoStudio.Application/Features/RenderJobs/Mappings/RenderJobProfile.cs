using AutoMapper;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Application.Features.RenderJobs.Mappings;

public class RenderJobProfile : Profile
{
    public RenderJobProfile()
    {
        CreateMap<RenderJob, RenderJobDto>();
        CreateMap<RenderJob, RenderJobSummaryDto>();
    }
}
