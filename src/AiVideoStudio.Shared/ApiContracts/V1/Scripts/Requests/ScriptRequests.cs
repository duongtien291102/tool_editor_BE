namespace AiVideoStudio.Shared.ApiContracts.V1.Scripts.Requests;

public class CreateScriptRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateScriptRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ExpectedVersion { get; set; }
}

public class AutoSaveScriptRequest
{
    // AutoSave could contain partial data, but since we are handling scene/elements individually,
    // this request might just update script top-level properties or handle full document save.
    // Given the minimal-update requirement, we might use this endpoint to auto-save script details.
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ExpectedVersion { get; set; }
}

public class GetScriptsByProjectQueryRequest
{
    public string? SearchTerm { get; set; }
    public bool IncludeDeleted { get; set; }
    public string? SortBy { get; set; }
    public bool Descending { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
