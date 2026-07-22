using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Projects.Commands;

public record DeleteProjectCommand(
    string Id
) : IRequest<Result<bool>>;
