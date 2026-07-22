namespace AiVideoStudio.Shared.DomainErrors;

public static class UserErrors
{
    public static readonly Error NotFound = new("USER.NOT_FOUND", "The user could not be found.");
    public static readonly Error EmailExists = new("USER.EMAIL_EXISTS", "The email is already registered.");
    public static readonly Error UsernameExists = new("USER.USERNAME_EXISTS", "The username is already taken.");
}
