namespace AiVideoStudio.Shared.DomainErrors;

public static class MediaErrors
{
    public static readonly Error NotFound = new("MEDIA.NOT_FOUND", "The media asset was not found.");
    public static readonly Error UnauthorizedAccess = new("MEDIA.UNAUTHORIZED_ACCESS", "You do not have permission to access or modify this media asset.");
    public static readonly Error InvalidFileType = new("MEDIA.INVALID_FILE_TYPE", "The uploaded file type or extension is not supported.");
    public static readonly Error FileTooLarge = new("MEDIA.FILE_TOO_LARGE", "The uploaded file exceeds the maximum allowed size limit.");
    public static readonly Error UploadFailed = new("MEDIA.UPLOAD_FAILED", "An error occurred while uploading the file to storage.");
    public static readonly Error ProjectNotFound = new("MEDIA.PROJECT_NOT_FOUND", "The specified project was not found.");
    public static readonly Error InvalidPayload = new("MEDIA.INVALID_PAYLOAD", "The media asset request payload is invalid.");
}
