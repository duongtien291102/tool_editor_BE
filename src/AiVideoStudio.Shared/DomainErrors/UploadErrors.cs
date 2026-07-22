namespace AiVideoStudio.Shared.DomainErrors;

public static class UploadErrors
{
    public static readonly Error NotFound = new("Upload.NotFound", "Upload session was not found.");
    public static readonly Error ProjectNotFound = new("Upload.ProjectNotFound", "Project was not found.");
    public static readonly Error Unauthorized = new("Upload.Unauthorized", "Authentication is required.");
    public static readonly Error Forbidden = new("Upload.Forbidden", "You cannot access this upload.");
    public static readonly Error InvalidState = new("Upload.InvalidState", "Upload state does not allow this operation.");
    public static readonly Error InvalidChunk = new("Upload.InvalidChunk", "Chunk validation failed.");
    public static readonly Error ChecksumMismatch = new("Upload.ChecksumMismatch", "Checksum verification failed.");
    public static readonly Error StorageFailure = new("Upload.StorageFailure", "Storage processing failed.");
}
