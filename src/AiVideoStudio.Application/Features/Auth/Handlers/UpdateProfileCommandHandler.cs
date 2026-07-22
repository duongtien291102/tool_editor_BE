using AiVideoStudio.Application.Features.Auth.Commands;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Application.Features.Auth.Handlers;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<bool>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result<bool>.Failure(UserErrors.NotFound);

        if (user.Version != request.Version)
        {
            return Result<bool>.Failure(GeneralErrors.ConcurrencyException);
        }

        if (user.Username != request.Username)
        {
            if (await _userRepository.ExistsUsernameAsync(request.Username, cancellationToken))
            {
                return Result<bool>.Failure(UserErrors.UsernameExists);
            }
            user.Username = request.Username;
        }

        user.Version++;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<bool>.Success(true);
    }
}
