using System.Security.Cryptography;
using AiVideoStudio.Application.Features.Uploads.DTOs;
using AiVideoStudio.Application.Storage;
using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Infrastructure.Storage;

public sealed class ChunkUploadEngine : IChunkUploadEngine
{
    private const string ChunkBucket = ".chunks";
    private readonly IStorageProvider _storage;
    public ChunkUploadEngine(IStorageProvider storage) => _storage = storage;
    public async Task<ChunkDto> StoreChunkAsync(UploadSession session, int index, byte[] data, string checksum, CancellationToken ct = default)
    {
        var actual = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
        if (!actual.Equals(checksum, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Chunk checksum mismatch.");
        await using var stream = new MemoryStream(data, false); await _storage.UploadAsync(ChunkBucket, $"{session.Id}/{index:D8}.part", stream, "application/octet-stream", ct);
        return new ChunkDto(index, data.LongLength, actual, true);
    }
    public async Task<string> MergeAsync(UploadSession session, CancellationToken ct = default)
    {
        var temp = Path.GetTempFileName();
        try
        {
            await using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                for (var i = 0; i < session.ChunkCount; i++) { await using var input = await _storage.OpenReadStreamAsync(ChunkBucket, $"{session.Id}/{i:D8}.part", ct); await input.CopyToAsync(output, ct); }
            await using var verify = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(verify, ct)).ToLowerInvariant(); if (!hash.Equals(session.Checksum, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("File checksum mismatch.");
            verify.Position = 0; var key = $"assets/{session.AssetId}{Path.GetExtension(session.FileName).ToLowerInvariant()}";
            return await _storage.UploadAsync(session.ProjectId, key, verify, session.ContentType, ct);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public async Task DeleteChunksAsync(string uploadId, CancellationToken ct = default)
    { for (var i = 0; i < 10000; i++) { var key = $"{uploadId}/{i:D8}.part"; if (!await _storage.ExistsAsync(ChunkBucket, key, ct)) break; await _storage.DeleteAsync(ChunkBucket, key, ct); } }
}
