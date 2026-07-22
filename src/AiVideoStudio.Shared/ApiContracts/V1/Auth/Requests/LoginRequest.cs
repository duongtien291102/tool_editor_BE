namespace AiVideoStudio.Shared.ApiContracts.V1.Auth.Requests;

public record LoginRequest(
    string Username,
    string Password,
    string? DeviceId
);
