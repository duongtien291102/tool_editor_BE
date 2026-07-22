namespace AiVideoStudio.Shared.ApiContracts.V1.Media.Requests;

public class UpdateMediaRequest
{
    public string? FileName { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? Duration { get; set; }
    public string? ThumbnailPath { get; set; }
}
