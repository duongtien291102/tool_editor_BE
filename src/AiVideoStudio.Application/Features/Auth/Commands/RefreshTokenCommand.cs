using AiVideoStudio.Shared.ApiContracts.V1.Auth.Responses;
using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string Token,
    string? DeviceId,
    string? UserAgent,
    string ClientIp
) : IRequest<Result<AuthResponse>>;
