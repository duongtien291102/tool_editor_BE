namespace AiVideoStudio.Shared.DomainErrors;

public static class AuthErrors
{
    public static readonly Error InvalidPassword = new("AUTH.INVALID_PASSWORD", "The password provided is invalid.");
    public static readonly Error UserNotFound = new("AUTH.USER_NOT_FOUND", "The user could not be found.");
    public static readonly Error Unauthorized = new("AUTH.UNAUTHORIZED", "Unauthorized access.");
    public static readonly Error InvalidCredentials = new("AUTH.INVALID_CREDENTIALS", "Invalid username or password.");
    public static readonly Error UserNotActive = new("AUTH.USER_NOT_ACTIVE", "The user account is not active.");
    public static readonly Error InvalidRefreshToken = new("AUTH.INVALID_REFRESH_TOKEN", "The refresh token is invalid or expired.");
    public static readonly Error InvalidToken = new("AUTH.INVALID_TOKEN", "The token is invalid or expired.");
    public static readonly Error PasswordReuse = new("AUTH.PASSWORD_REUSE", "The new password must be different from recent passwords.");
}
