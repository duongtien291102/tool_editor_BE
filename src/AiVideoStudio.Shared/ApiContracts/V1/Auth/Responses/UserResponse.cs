using System.Collections.Generic;

namespace AiVideoStudio.Shared.ApiContracts.V1.Auth.Responses;

public record UserResponse(
    string Id,
    string Username,
    string Email,
    string Status,
    IEnumerable<string> Roles
);
