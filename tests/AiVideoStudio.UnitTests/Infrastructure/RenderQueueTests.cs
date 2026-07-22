using System;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Application.Features.RenderJobs.DTOs;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Infrastructure.Render;
using FluentAssertions;
using Xunit;

namespace AiVideoStudio.UnitTests.Infrastructure;

public class RenderQueueTests
{
    private readonly InMemoryRenderQueue _queue;

    public RenderQueueTests()
    {
        _queue = new InMemoryRenderQueue();
    }

    [Fact]
    public async Task Enqueue_Should_Increase_Count()
    {
        await _queue.EnqueueAsync(new QueueItem("1", RenderPriority.Normal, DateTimeOffset.UtcNow));
        _queue.Count.Should().Be(1);
    }

    [Fact]
    public async Task Dequeue_Should_Return_Highest_Priority_First()
    {
        await _queue.EnqueueAsync(new QueueItem("low", RenderPriority.Low, DateTimeOffset.UtcNow));
        await _queue.EnqueueAsync(new QueueItem("critical", RenderPriority.Critical, DateTimeOffset.UtcNow));
        await _queue.EnqueueAsync(new QueueItem("high", RenderPriority.High, DateTimeOffset.UtcNow));

        var first = await _queue.DequeueAsync();
        first!.JobId.Should().Be("critical");

        var second = await _queue.DequeueAsync();
        second!.JobId.Should().Be("high");

        var third = await _queue.DequeueAsync();
        third!.JobId.Should().Be("low");
    }

    [Fact]
    public async Task Dequeue_Should_Return_Oldest_Created_Within_Same_Priority()
    {
        var now = DateTimeOffset.UtcNow;
        await _queue.EnqueueAsync(new QueueItem("newer", RenderPriority.Normal, now.AddMinutes(5)));
        await _queue.EnqueueAsync(new QueueItem("older", RenderPriority.Normal, now));

        var first = await _queue.DequeueAsync();
        first!.JobId.Should().Be("older");

        var second = await _queue.DequeueAsync();
        second!.JobId.Should().Be("newer");
    }

    [Fact]
    public async Task RemoveAsync_Should_Remove_Item_By_JobId()
    {
        await _queue.EnqueueAsync(new QueueItem("j1", RenderPriority.Normal, DateTimeOffset.UtcNow));
        await _queue.EnqueueAsync(new QueueItem("j2", RenderPriority.Normal, DateTimeOffset.UtcNow));
        
        _queue.Count.Should().Be(2);

        await _queue.RemoveAsync("j1");
        
        _queue.Count.Should().Be(1);
        var remaining = await _queue.DequeueAsync();
        remaining!.JobId.Should().Be("j2");
    }
}
