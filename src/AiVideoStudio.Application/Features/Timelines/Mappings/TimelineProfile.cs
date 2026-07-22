using AiVideoStudio.Application.Features.Timelines.DTOs;
using AiVideoStudio.Domain.Entities;
using AutoMapper;

namespace AiVideoStudio.Application.Features.Timelines.Mappings;

public class TimelineProfile : Profile
{
    public TimelineProfile()
    {
        CreateMap<Timeline, TimelineDto>();
        CreateMap<Track, TrackDto>();
        CreateMap<Clip, ClipDto>();
    }
}
