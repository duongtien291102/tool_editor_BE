using System.ComponentModel.DataAnnotations;

namespace AiVideoStudio.Application.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string RefreshTokenSecret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; set; }

    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; set; }
}
