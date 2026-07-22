using AiVideoStudio.Shared.Responses;
using MediatR;

namespace AiVideoStudio.Application.Features.Auth.Commands;

public record VerifyEmailCommand(
    string Token,
    string ClientIp) : IRequest<Result<bool>>;
