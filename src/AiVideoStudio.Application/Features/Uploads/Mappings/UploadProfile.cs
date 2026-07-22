using AiVideoStudio.Application.Features.Uploads.DTOs;
using AiVideoStudio.Domain.Entities;
using AutoMapper;
namespace AiVideoStudio.Application.Features.Uploads.Mappings;
public sealed class UploadProfile : Profile
{
    public UploadProfile()
    {
        CreateMap<UploadSession, UploadSessionDto>();
        CreateMap<UploadSession, UploadSummaryDto>().ConstructUsing(s => new UploadSummaryDto(
            s.Id, s.AssetId, s.ProjectId, s.Status, s.FileName, s.FileSize, s.UploadedBytes,
            s.ChunkCount, s.CompletedChunks.Count, s.CreatedAt, s.CompletedAt));
    }
}
