using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities;

public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
