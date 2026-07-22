using AiVideoStudio.Application.Features.Exports.DTOs;
using AiVideoStudio.Domain.Entities;
using AutoMapper;

namespace AiVideoStudio.Application.Features.Exports.Mappings;

public sealed class ExportProfile : Profile
{
    public ExportProfile()
    {
        CreateMap<ExportJob, ExportJobDto>();
        CreateMap<ExportJob, ExportSummaryDto>();
    }
}
