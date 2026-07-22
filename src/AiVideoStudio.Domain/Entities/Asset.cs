using AiVideoStudio.Domain.Base;

namespace AiVideoStudio.Domain.Entities;

public class Asset : BaseEntity
{
    public string StorageProvider { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
