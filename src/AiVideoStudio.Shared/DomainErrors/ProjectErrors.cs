namespace AiVideoStudio.Shared.DomainErrors;

public static class ProjectErrors
{
    public static readonly Error NotFound = new("PROJECT.NOT_FOUND", "The project was not found.");
    public static readonly Error UnauthorizedAccess = new("PROJECT.UNAUTHORIZED_ACCESS", "You do not have permission to access or modify this project.");
    public static readonly Error NameRequired = new("PROJECT.NAME_REQUIRED", "Project name is required.");
    public static readonly Error InvalidPayload = new("PROJECT.INVALID_PAYLOAD", "The project request payload is invalid.");
}


