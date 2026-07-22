using System.Collections.Generic;

namespace AiVideoStudio.Shared.Models.Filters;

public class FilterGroup
{
    public string Logic { get; set; } = "and"; // or "or"
    public List<FilterRule> Rules { get; set; } = new();
    public List<FilterGroup> Groups { get; set; } = new();
}
