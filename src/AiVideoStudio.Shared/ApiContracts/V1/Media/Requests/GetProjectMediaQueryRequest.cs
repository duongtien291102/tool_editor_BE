namespace AiVideoStudio.Shared.ApiContracts.V1.Media.Requests;

public class GetProjectMediaQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public string? AssetType { get; set; }
    public string? Status { get; set; }
}
