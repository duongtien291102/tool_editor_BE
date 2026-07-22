using AiVideoStudio.Domain.Enums;

namespace AiVideoStudio.Application.Features.Exports.Models;

public sealed record FFmpegInputAsset(string AssetId, string SourcePath, TimeSpan Offset, TimeSpan Duration);
public sealed record VideoFilter(string NodeId, string FilterType, IReadOnlyDictionary<string, string> Parameters);
public sealed record AudioFilter(string NodeId, double Volume, TimeSpan Offset);
public sealed record SubtitleFilter(string NodeId, string SourcePath, TimeSpan Offset);
public sealed record TransitionFilter(string FromNodeId, string ToNodeId, TimeSpan Duration);
public sealed record OverlayFilter(string NodeId, int Layer, TimeSpan Start, TimeSpan Duration);

public sealed record FFmpegOutputOptions(
    string OutputDirectory,
    string FileName,
    string Resolution,
    double FrameRate,
    VideoCodec VideoCodec,
    AudioCodec AudioCodec,
    ContainerFormat Container);

public sealed record FFmpegCommandModel(
    IReadOnlyList<FFmpegInputAsset> InputAssets,
    IReadOnlyList<VideoFilter> VideoFilters,
    IReadOnlyList<AudioFilter> AudioFilters,
    IReadOnlyList<SubtitleFilter> SubtitleFilters,
    IReadOnlyList<TransitionFilter> Transitions,
    IReadOnlyList<OverlayFilter> Overlays,
    FFmpegOutputOptions OutputOptions);

public sealed record ExportProgressUpdate(ExportStatus Status, int Progress);
public sealed record ExportProviderResult(bool IsSuccess, string? OutputPath, string? ErrorCode, string? ErrorMessage)
{
    public static ExportProviderResult Success(string path) => new(true, path, null, null);
    public static ExportProviderResult Failure(string code, string message) => new(false, null, code, message);
}
