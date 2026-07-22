using System.Text;
using System.Text.Json;
using AiVideoStudio.Application.Features.Uploads.DTOs;
using AiVideoStudio.Application.Storage;
using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Infrastructure.Storage;

public sealed class MockThumbnailGenerator : IThumbnailGenerator
{
    private readonly IStorageProvider _storage; public MockThumbnailGenerator(IStorageProvider storage) => _storage = storage;
    public Task<string> GenerateImageThumbnailAsync(string p, string a, string s, CancellationToken ct = default) => Generate(p, a, "thumbnail-image.txt", s, ct);
    public Task<string> GenerateVideoThumbnailAsync(string p, string a, string s, CancellationToken ct = default) => Generate(p, a, "thumbnail-video.txt", s, ct);
    public Task<string> GenerateAudioWaveformAsync(string p, string a, string s, CancellationToken ct = default) => Generate(p, a, "waveform-audio.txt", s, ct);
    private async Task<string> Generate(string p, string a, string name, string source, CancellationToken ct) { await using var data = new MemoryStream(Encoding.UTF8.GetBytes($"mock:{source}")); return await _storage.UploadAsync(p, $"derived/{a}/{name}", data, "text/plain", ct); }
}
public sealed class MockMetadataExtractor : IMetadataExtractor
{
    public Task<AssetMetadataDto> ExtractImageAsync(Stream s, CancellationToken ct = default) => Result(s, "image", 1920, 1080, null, ct);
    public Task<AssetMetadataDto> ExtractVideoAsync(Stream s, CancellationToken ct = default) => Result(s, "video", 1920, 1080, 60, ct);
    public Task<AssetMetadataDto> ExtractAudioAsync(Stream s, CancellationToken ct = default) => Result(s, "audio", null, null, 60, ct);
    public Task<AssetMetadataDto> ExtractSubtitleAsync(Stream s, CancellationToken ct = default) => Result(s, "subtitle", null, null, null, ct);
    private static Task<AssetMetadataDto> Result(Stream s, string kind, int? w, int? h, double? d, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(new AssetMetadataDto(w, h, d, kind, new Dictionary<string, string> { { "mock", "true" }, { "length", (s.CanSeek ? s.Length : 0).ToString() } })); }
}
public sealed class AssetManifestBuilder : IAssetManifestBuilder
{
    private readonly IStorageProvider _storage; public AssetManifestBuilder(IStorageProvider storage) => _storage = storage;
    public ManifestDto Build(UploadSession s, string path, string? thumb, AssetMetadataDto meta) => new(s.AssetId, s.FileName, s.ContentType, s.FileSize, s.Checksum, path, thumb, meta, DateTimeOffset.UtcNow);
    public async Task<string> SaveAsync(string projectId, ManifestDto manifest, CancellationToken ct = default) { var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }); await using var data = new MemoryStream(Encoding.UTF8.GetBytes(json)); return await _storage.UploadAsync(projectId, $"manifests/{manifest.AssetId}.json", data, "application/json", ct); }
}
