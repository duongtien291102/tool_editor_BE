namespace AiVideoStudio.Application.Auth;

public interface IAuthService
{
    Task<string?> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
