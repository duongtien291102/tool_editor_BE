using AiVideoStudio.Application.Features.Uploads.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;
namespace AiVideoStudio.Application.Features.Uploads;
public record StartUploadCommand(string ProjectId, string FileName, string ContentType, long FileSize, int ChunkCount, string Checksum) : IRequest<Result<UploadSessionDto>>;
public record UploadChunkCommand(string UploadId, int ChunkIndex, byte[] Data, string Checksum) : IRequest<Result<ChunkDto>>;
public record CompleteUploadCommand(string UploadId) : IRequest<Result<UploadSessionDto>>;
public record CancelUploadCommand(string UploadId) : IRequest<Result>;
public record RetryUploadCommand(string UploadId) : IRequest<Result<UploadSessionDto>>;
