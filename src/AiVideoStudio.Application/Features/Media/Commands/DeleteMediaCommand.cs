using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Media.Commands;

public record DeleteMediaCommand(
    string Id
) : IRequest<Result<bool>>;
