using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities;

public class PasswordHistory : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "BCrypt";
    public int CostFactor { get; set; } = 11;
}
