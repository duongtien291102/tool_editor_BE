namespace AiVideoStudio.Shared.ApiContracts.V1.Auth.Requests;

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string DeviceId);
