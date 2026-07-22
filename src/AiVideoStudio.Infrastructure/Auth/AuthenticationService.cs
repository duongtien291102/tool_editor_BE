using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthenticationService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<User>> VerifyCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByUsernameAsync(username, cancellationToken);
        if (user == null)
        {
            return Result<User>.Failure(AuthErrors.InvalidCredentials);
        }

        if (user.Status != UserStatus.Active)
        {
            return Result<User>.Failure(AuthErrors.UserNotActive);
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return Result<User>.Failure(AuthErrors.InvalidCredentials);
        }

        return Result<User>.Success(user);
    }
}
