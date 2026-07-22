namespace AiVideoStudio.Shared.ApiContracts.V1.Projects.Requests;

public record UpdateProjectRequest(
    string Name,
    string? Description = null,
    string? Thumbnail = null,
    string? Status = null
);
