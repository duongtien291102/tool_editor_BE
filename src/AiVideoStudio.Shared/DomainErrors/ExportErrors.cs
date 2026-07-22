namespace AiVideoStudio.Shared.DomainErrors;

public static class ExportErrors
{
    public static readonly Error NotFound = new("Export.NotFound", "The export job was not found.");
    public static readonly Error RenderJobNotFound = new("Export.RenderJobNotFound", "The source render job was not found.");
    public static readonly Error ProjectNotFound = new("Export.ProjectNotFound", "The project was not found.");
    public static readonly Error TimelineNotFound = new("Export.TimelineNotFound", "The timeline was not found.");
    public static readonly Error Unauthorized = new("Export.Unauthorized", "Authentication is required.");
    public static readonly Error Forbidden = new("Export.Forbidden", "You are not allowed to access this export.");
    public static readonly Error InvalidTransition = new("Export.InvalidTransition", "The export status transition is not allowed.");
    public static readonly Error MaxRetriesReached = new("Export.MaxRetriesReached", "The maximum export retry count was reached.");
    public static readonly Error AssetNotFound = new("Export.AssetNotFound", "A timeline asset was not found.");
}
