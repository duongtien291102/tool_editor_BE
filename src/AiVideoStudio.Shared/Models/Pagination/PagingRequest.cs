namespace AiVideoStudio.Shared.Models.Pagination;

public class PagingRequest
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
