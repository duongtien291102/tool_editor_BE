namespace AiVideoStudio.Shared.ApiContracts.V1.Auth.Responses;

public record AuthResponse(
    string AccessToken,
    long ExpiresIn,
    string? RefreshToken
);
