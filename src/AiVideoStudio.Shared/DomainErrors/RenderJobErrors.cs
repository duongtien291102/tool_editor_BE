namespace AiVideoStudio.Shared.DomainErrors;

public static class RenderJobErrors
{
    public static readonly Error NotFound = new(
        "RenderJob.NotFound",
        "The render job was not found.");

    public static readonly Error Unauthorized = new(
        "RenderJob.Unauthorized",
        "You are not authorized to access this render job.");

    public static readonly Error InvalidStatusTransition = new(
        "RenderJob.InvalidStatusTransition",
        "The requested status transition is not allowed.");

    public static readonly Error AlreadyCompleted = new(
        "RenderJob.AlreadyCompleted",
        "This render job has already completed and cannot be modified.");

    public static readonly Error AlreadyCancelled = new(
        "RenderJob.AlreadyCancelled",
        "This render job has already been cancelled.");

    public static readonly Error MaxRetriesReached = new(
        "RenderJob.MaxRetriesReached",
        "The maximum number of retries has been reached for this render job.");

    public static readonly Error CannotRetry = new(
        "RenderJob.CannotRetry",
        "This render job cannot be retried in its current state.");

    public static readonly Error CannotCancel = new(
        "RenderJob.CannotCancel",
        "This render job cannot be cancelled in its current state.");

    public static readonly Error InvalidProgress = new(
        "RenderJob.InvalidProgress",
        "Progress must be between 0 and 100.");

    public static readonly Error ProjectNotFound = new(
        "RenderJob.ProjectNotFound",
        "The project associated with this render job was not found.");
}
