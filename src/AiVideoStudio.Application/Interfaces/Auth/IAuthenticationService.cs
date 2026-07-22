using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Shared.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Interfaces.Auth;

public interface IAuthenticationService
{
    Task<Result<User>> VerifyCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}
