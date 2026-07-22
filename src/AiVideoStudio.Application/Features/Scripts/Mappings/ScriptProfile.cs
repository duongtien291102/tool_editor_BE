using AiVideoStudio.Application.Features.Scripts.DTOs;
using AiVideoStudio.Domain.Entities;
using AutoMapper;

namespace AiVideoStudio.Application.Features.Scripts.Mappings;

public sealed class ScriptProfile : Profile
{
    public ScriptProfile()
    {
        CreateMap<Script, ScriptDto>();
        CreateMap<Script, ScriptSummaryDto>();
        CreateMap<Scene, SceneDto>();
        CreateMap<SceneElement, SceneElementDto>();
    }
}
