using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Auth.Commands;

public record ForgotPasswordCommand(
    string Email,
    string ClientIp) : IRequest<Result<bool>>;
