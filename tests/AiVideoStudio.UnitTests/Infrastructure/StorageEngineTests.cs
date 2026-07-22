using System.Security.Cryptography;
using System.Text;
using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Features.Uploads.DTOs;
using AiVideoStudio.Application.Features.Uploads;
using AiVideoStudio.Application.Features.Uploads.Validators;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Infrastructure.Mongo.MongoConventions;
using AiVideoStudio.Infrastructure.Storage;
using AiVideoStudio.Infrastructure.Mongo.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NSubstitute;
using Xunit;
namespace AiVideoStudio.UnitTests.Infrastructure;

public class StorageEngineTests
{
    [Fact]
    public async Task MockStorage_ShouldUploadDownloadCopyMoveDeleteAndTemporaryUrl()
    { var root = Temp(); try { var s = Provider(root); await using var d = new MemoryStream(Encoding.UTF8.GetBytes("data")); await s.UploadAsync("p", "a.txt", d, "text/plain"); (await s.ExistsAsync("p", "a.txt")).Should().BeTrue(); await s.CopyAsync("p", "a.txt", "p", "b.txt"); await s.MoveAsync("p", "b.txt", "p", "c.txt"); await using (var r = await s.OpenReadStreamAsync("p", "c.txt")) { r.Length.Should().Be(4); } (await s.GenerateTemporaryUrlAsync("p", "c.txt", TimeSpan.FromMinutes(1))).Scheme.Should().Be("mock-storage"); await s.DeleteAsync("p", "c.txt"); } finally { if (Directory.Exists(root)) Directory.Delete(root, true); } }
    [Fact]
    public async Task ChunkEngine_ShouldValidateResumeMergeAndWholeChecksum()
    { var root = Temp(); try { var storage = Provider(root); var engine = new ChunkUploadEngine(storage); var bytes = Encoding.UTF8.GetBytes("abcdef"); var all = Hash(bytes); var session = UploadSession.Create("p", "o", "x.mp4", "video/mp4", 6, 2, all); var a = bytes[..3]; var b = bytes[3..]; await engine.StoreChunkAsync(session, 0, a, Hash(a)); session.UploadChunk(0, 3); await engine.StoreChunkAsync(session, 1, b, Hash(b)); session.UploadChunk(1, 3); session.Merge(); var path = await engine.MergeAsync(session); await using var file = await storage.OpenReadStreamAsync("", path); using var reader = new StreamReader(file); (await reader.ReadToEndAsync()).Should().Be("abcdef"); await engine.DeleteChunksAsync(session.Id); } finally { if (Directory.Exists(root)) Directory.Delete(root, true); } }
    [Fact] public async Task ChunkEngine_ShouldRejectBadChecksum() { var root = Temp(); try { var engine = new ChunkUploadEngine(Provider(root)); var s = UploadSession.Create("p", "o", "x.mp4", "video/mp4", 1, 1, new string('a', 64)); var a = () => engine.StoreChunkAsync(s, 0, new byte[] { 1 }, new string('b', 64)); await a.Should().ThrowAsync<InvalidDataException>(); } finally { if (Directory.Exists(root)) Directory.Delete(root, true); } }
    [Fact]
    public async Task ThumbnailMetadataAndManifest_ShouldProduceMockArtifacts()
    { var root = Temp(); try { var storage = Provider(root); var thumbs = new MockThumbnailGenerator(storage); var thumb = await thumbs.GenerateVideoThumbnailAsync("p", "a", "source"); (await storage.ExistsAsync("", thumb)).Should().BeTrue(); var extractor = new MockMetadataExtractor(); await using var data = new MemoryStream(new byte[10]); var meta = await extractor.ExtractVideoAsync(data); meta.Kind.Should().Be("video"); var session = UploadSession.Create("p", "o", "x.mp4", "video/mp4", 10, 1, new string('a', 64)); var builder = new AssetManifestBuilder(storage); var manifest = builder.Build(session, "source", thumb, meta); var path = await builder.SaveAsync("p", manifest); (await storage.ExistsAsync("", path)).Should().BeTrue(); } finally { if (Directory.Exists(root)) Directory.Delete(root, true); } }
    [Fact] public void Validators_ShouldRejectInvalidContracts() { new StartUploadValidator().Validate(new StartUploadCommand("", "", "", 0, 0, "x")).IsValid.Should().BeFalse(); new UploadChunkValidator().Validate(new UploadChunkCommand("", -1, Array.Empty<byte>(), "x")).IsValid.Should().BeFalse(); new CompleteUploadValidator().Validate(new CompleteUploadCommand("")).IsValid.Should().BeFalse(); }
    [Fact]
    public void UploadSession_BsonRoundTrip_ShouldRestoreCompletedChunks()
    {
        MongoConventionPackInitializer.Initialize();
        var session = UploadSession.Create("p", "o", "x.mp4", "video/mp4", 3, 1, new string('a', 64));
        session.UploadChunk(0, 3);

        var restored = BsonSerializer.Deserialize<UploadSession>(session.ToBson());

        restored.CompletedChunks.Should().Equal(0);
        restored.UploadedBytes.Should().Be(3);
        restored.Invoking(value => value.Merge()).Should().NotThrow();
    }
    [Fact] public async Task Repository_ShouldInsertSession() { var c = Substitute.For<IMongoCollection<UploadSession>>(); var r = new UploadSessionRepository(c); var s = UploadSession.Create("p", "o", "x.mp4", "video/mp4", 1, 1, new string('a', 64)); await r.AddAsync(s); await c.Received().InsertOneAsync(s, Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>()); }
    private static string Hash(byte[] b) => Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant(); private static string Temp() => Path.Combine(Path.GetTempPath(), $"storage-{Guid.NewGuid():N}");
    private static MockStorageProvider Provider(string root) => new(Options.Create(new StorageOptions { BasePath = root, Provider = "Mock" }));
}
