using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AiVideoStudio.Application.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string Provider { get; set; } = "Local";

    [Required]
    public string BasePath { get; set; } = "./uploads";

    public long MaxFileSizeBytes { get; set; } = 524288000; // 500 MB default

    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg",
        ".mp4", ".mov", ".avi", ".mkv", ".webm",
        ".mp3", ".wav", ".aac", ".m4a", ".ogg", ".flac",
        ".vtt", ".srt", ".json",
        ".ttf", ".otf", ".woff", ".woff2"
    };

    public List<string> AllowedMimeTypes { get; set; } = new()
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
        "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm",
        "audio/mpeg", "audio/wav", "audio/x-wav", "audio/aac", "audio/mp4", "audio/ogg", "audio/flac",
        "text/vtt", "application/x-subrip", "text/plain", "application/json",
        "font/ttf", "font/otf", "font/woff", "font/woff2",
        "application/octet-stream"
    };
}
