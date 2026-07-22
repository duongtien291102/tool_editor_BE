using AiVideoStudio.Domain.Base;
using System.Collections.Generic;

namespace AiVideoStudio.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> PermissionIds { get; set; } = new();
}
