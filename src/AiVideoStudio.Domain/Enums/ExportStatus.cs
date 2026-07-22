namespace AiVideoStudio.Domain.Enums;

public enum ExportStatus
{
    Pending,
    Preparing,
    Rendering,
    Muxing,
    Completed,
    Failed,
    Cancelled
}
