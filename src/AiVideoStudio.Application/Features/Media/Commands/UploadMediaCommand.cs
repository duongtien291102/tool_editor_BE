using AiVideoStudio.Application.Features.Media.DTOs;
using AiVideoStudio.Shared.Responses;
using MediatR;
using System.IO;

namespace AiVideoStudio.Application.Features.Media.Commands;

public record UploadMediaCommand(
    string ProjectId,
    string FileName,
    string MimeType,
    long FileSize,
    Stream ContentStream
) : IRequest<Result<MediaDto>>;
