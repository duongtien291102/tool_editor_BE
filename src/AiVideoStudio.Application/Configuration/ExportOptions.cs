using System.ComponentModel.DataAnnotations;

namespace AiVideoStudio.Application.Configuration;

public sealed class ExportOptions
{
    public const string SectionName = "Export";

    [Required]
    public string OutputDirectory { get; set; } = string.Empty;

    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 60;

    [Range(0, 10)]
    public int RetryCount { get; set; } = 1;

    [Range(1, 10000)]
    public int MockStepDelayMilliseconds { get; set; } = 20;
}
