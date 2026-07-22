namespace AiVideoStudio.Shared.ApiContracts.V1.Auth.Requests;

public record UpdateProfileRequest(
    string Username,
    int Version);
