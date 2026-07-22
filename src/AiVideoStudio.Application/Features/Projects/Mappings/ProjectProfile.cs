using AiVideoStudio.Application.Features.Projects.DTOs;
using AiVideoStudio.Domain.Entities;
using AutoMapper;

namespace AiVideoStudio.Application.Features.Projects.Mappings;

public class ProjectProfile : Profile
{
    public ProjectProfile()
    {
        CreateMap<Project, ProjectDto>();
    }
}
