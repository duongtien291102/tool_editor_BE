namespace AiVideoStudio.Shared.ApiContracts.V1.Auth.Requests;

public record ResetPasswordRequest(
    string Token,
    string NewPassword);
