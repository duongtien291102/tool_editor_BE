namespace AiVideoStudio.Shared.DomainErrors;

public static class WorkspaceErrors
{
    public static readonly Error NotFound = new("WORKSPACE.NOT_FOUND", "The workspace was not found.");
}
