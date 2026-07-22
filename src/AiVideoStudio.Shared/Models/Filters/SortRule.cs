namespace AiVideoStudio.Shared.Models.Filters;

public class SortRule
{
    public string Field { get; set; } = string.Empty;
    public string Direction { get; set; } = "asc"; // or "desc"
}
