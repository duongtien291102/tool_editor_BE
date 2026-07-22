using System.ComponentModel.DataAnnotations;

namespace AiVideoStudio.Application.Configuration;

public sealed class ProviderOptions
{
    public const string SectionName = "Providers";

    public bool Enabled { get; set; } = true;

    [Range(1, 3600)]
    public int Timeout { get; set; } = 30;

    [Range(0, 10)]
    public int Retry { get; set; } = 1;

    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Model { get; set; }
}
