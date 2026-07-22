using AiVideoStudio.Application.Interfaces.Auth;

namespace AiVideoStudio.Infrastructure.Auth;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, 12);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, hashedPassword);
    }

    public bool NeedsRehash(string hashedPassword)
    {
        return BCrypt.Net.BCrypt.PasswordNeedsRehash(hashedPassword, 12);
    }
}
