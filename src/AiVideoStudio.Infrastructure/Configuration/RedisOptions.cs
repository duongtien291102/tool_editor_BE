using System.ComponentModel.DataAnnotations;

namespace AiVideoStudio.Infrastructure.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}
