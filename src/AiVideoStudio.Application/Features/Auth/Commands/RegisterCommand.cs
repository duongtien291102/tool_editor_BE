using AiVideoStudio.Shared.ApiContracts.V1.Auth.Responses;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Auth.Commands;

public record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string ClientIp,
    string DeviceId,
    string UserAgent) : IRequest<Result<AuthResponse>>;
