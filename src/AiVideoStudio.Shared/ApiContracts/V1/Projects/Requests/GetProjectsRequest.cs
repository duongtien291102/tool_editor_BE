namespace AiVideoStudio.Shared.ApiContracts.V1.Projects.Requests;

public record GetProjectsRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false,
    string? Status = null
);
