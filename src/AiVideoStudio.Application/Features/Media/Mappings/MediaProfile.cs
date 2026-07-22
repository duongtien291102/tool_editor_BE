using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Domain.Entities;
using AutoMapper;

namespace AiVideoStudio.Application.Features.Media.Mappings;

public class MediaProfile : Profile
{
    public MediaProfile()
    {
        CreateMap<MediaAsset, MediaDto>();
    }
}
