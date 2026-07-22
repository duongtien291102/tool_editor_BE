using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Auth.Commands;

public record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ClientIp) : IRequest<Result<bool>>;
