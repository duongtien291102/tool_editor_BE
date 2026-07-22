using System.Collections.Generic;

namespace AiVideoStudio.Shared.Models.Pagination;

public class PagedResult<T>
{
    public int TotalItems { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling((double)TotalItems / PageSize) : 0;
    public List<T> Items { get; set; } = new();

    public PagedResult(List<T> items, int totalItems, int pageIndex, int pageSize)
    {
        Items = items;
        TotalItems = totalItems;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
