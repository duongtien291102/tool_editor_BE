using AiVideoStudio.Domain.Entities;
using System.Collections.Generic;

namespace AiVideoStudio.Application.Interfaces.Auth;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, IEnumerable<string> roles);
}
