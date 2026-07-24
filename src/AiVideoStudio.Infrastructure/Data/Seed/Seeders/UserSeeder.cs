using AiVideoStudio.Application.Interfaces.Auth;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Data.Seed.Seeders;

public class UserSeeder : ISeeder
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserSeeder(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingAdmin = await _userRepository.FindByUsernameAsync("admin", cancellationToken);
        if (existingAdmin == null)
        {
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = _passwordHasher.HashPassword("123"),
                Status = UserStatus.Active,
                EmailVerifiedAt = System.DateTimeOffset.UtcNow,
                CreatedBy = "System"
            };
            await _userRepository.AddAsync(adminUser, cancellationToken);
        }
    }
}
