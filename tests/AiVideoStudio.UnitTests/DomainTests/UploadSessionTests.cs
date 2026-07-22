using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Uploads;
using FluentAssertions;
using Xunit;
namespace AiVideoStudio.UnitTests.DomainTests;

public class UploadSessionTests
{
    private static UploadSession New(long size = 6, int chunks = 2) => UploadSession.Create("p", "owner", "file.mp4", "video/mp4", size, chunks, new string('a', 64));
    [Fact] public void Create_ShouldInitializePendingAndEvent() { var s = New(); s.Status.Should().Be(UploadStatus.Pending); s.Version.Should().Be(1); s.DomainEvents.Should().ContainSingle(x => x is UploadStartedEvent); }
    [Fact] public void Chunks_ShouldSupportResumeAndProgress() { var s = New(); s.UploadChunk(0, 3); s.UploadChunk(0, 3); s.UploadedBytes.Should().Be(3); s.CompletedChunks.Should().Equal(0); s.Status.Should().Be(UploadStatus.Uploading); }
    [Fact] public void Merge_ShouldRequireEveryChunkAndByte() { var s = New(); s.UploadChunk(0, 3); var a = () => s.Merge(); a.Should().Throw<InvalidOperationException>(); s.UploadChunk(1, 3); s.Merge(); s.Status.Should().Be(UploadStatus.Merging); }
    [Fact] public void Complete_ShouldSetPathsAndVersion() { var s = New(); s.UploadChunk(0, 3); s.UploadChunk(1, 3); s.Merge(); s.Complete("asset", "manifest"); s.Status.Should().Be(UploadStatus.Completed); s.StoragePath.Should().Be("asset"); s.DomainEvents.Should().Contain(x => x is UploadCompletedEvent); }
    [Fact] public void CancelAndRetry_ShouldEnforceState() { var c = New(); c.Cancel(); c.Status.Should().Be(UploadStatus.Cancelled); var s = New(); s.Fail("x"); s.Retry(); s.Status.Should().Be(UploadStatus.Pending); s.RetryCount.Should().Be(1); }
    [Fact] public void Chunk_ShouldRejectInvalidIndexAndOverflow() { var s = New(); Action invalidIndex = () => s.UploadChunk(2, 1); invalidIndex.Should().Throw<ArgumentOutOfRangeException>(); Action overflow = () => s.UploadChunk(0, 7); overflow.Should().Throw<ArgumentOutOfRangeException>(); }
}
