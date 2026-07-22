using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Auth.Commands;

public record UpdateProfileCommand(
    string UserId,
    string Username,
    int Version) : IRequest<Result<bool>>;
