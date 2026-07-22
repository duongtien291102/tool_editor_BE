using System.ComponentModel.DataAnnotations;

namespace AiVideoStudio.Infrastructure.Configuration;

public class MongoOptions
{
    public const string SectionName = "MongoDb";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;
}
