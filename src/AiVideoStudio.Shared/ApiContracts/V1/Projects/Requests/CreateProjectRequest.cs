namespace AiVideoStudio.Shared.ApiContracts.V1.Projects.Requests;

public record CreateProjectRequest(
    string Name,
    string? Description = null,
    string? Thumbnail = null
);
