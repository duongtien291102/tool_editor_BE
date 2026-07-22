using System;
using System.Text.Json;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace AiVideoStudio.UnitTests.DomainTests;

public class RenderJobTests
{
    private readonly string _projectId = "project-1";
    private readonly string _ownerId = "owner-1";

    private RenderJob CreateTestJob(RenderPriority priority = RenderPriority.Normal)
    {
        return RenderJob.Create(_projectId, _ownerId, RenderJobType.RenderTimeline, RenderProvider.Internal, priority, maxRetryCount: 2);
    }

    [Fact]
    public void Create_Should_Initialize_In_Pending_Status()
    {
        var job = CreateTestJob();

        job.Status.Should().Be(RenderJobStatus.Pending);
        job.Progress.Should().Be(0);
        job.Priority.Should().Be(RenderPriority.Normal);
        job.RetryCount.Should().Be(0);
        job.MaxRetryCount.Should().Be(2);
    }

    [Fact]
    public void Queue_From_Pending_Should_Succeed()
    {
        var job = CreateTestJob();
        job.Queue();

        job.Status.Should().Be(RenderJobStatus.Queued);
    }

    [Fact]
    public void Start_From_Queued_Should_Succeed()
    {
        var job = CreateTestJob();
        job.Queue();
        job.Start();

        job.Status.Should().Be(RenderJobStatus.Processing);
        job.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateProgress_While_Processing_Should_Update_Value()
    {
        var job = CreateTestJob();
        job.Queue();
        job.Start();
        
        job.UpdateProgress(50);

        job.Progress.Should().Be(50);
    }

    [Fact]
    public void UpdateProgress_Should_Throw_If_Out_Of_Range()
    {
        var job = CreateTestJob();
        job.Queue();
        job.Start();

        Action act1 = () => job.UpdateProgress(-1);
        Action act2 = () => job.UpdateProgress(101);

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Complete_From_Processing_Should_Succeed_And_Set_Progress_100()
    {
        var job = CreateTestJob();
        job.Queue();
        job.Start();
        
        job.Complete(null);

        job.Status.Should().Be(RenderJobStatus.Completed);
        job.Progress.Should().Be(100);
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Fail_From_Processing_Should_Succeed_And_Keep_Progress()
    {
        var job = CreateTestJob();
        job.Queue();
        job.Start();
        job.UpdateProgress(35);
        
        job.Fail("Something went wrong");

        job.Status.Should().Be(RenderJobStatus.Failed);
        job.Progress.Should().Be(35);
        job.ErrorMessage.Should().Be("Something went wrong");
    }

    [Fact]
    public void Retry_From_Failed_Should_Queue_And_Increment_Counter()
    {
        var job = CreateTestJob();
        job.Queue();
        job.Start();
        job.Fail("Error");
        
        job.Retry("admin-1");

        job.Status.Should().Be(RenderJobStatus.Queued);
        job.RetryCount.Should().Be(1);
        job.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Retry_Should_Throw_If_MaxRetries_Reached()
    {
        var job = CreateTestJob();
        job.Queue();
        job.Start();
        job.Fail("Error");
        job.Retry("admin-1"); // 1
        job.Start();
        job.Fail("Error");
        job.Retry("admin-1"); // 2
        job.Start();
        job.Fail("Error");
        
        Action act = () => job.Retry("admin-1");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Max retry count*");
    }

    [Theory]
    [InlineData(RenderJobStatus.Completed)]
    [InlineData(RenderJobStatus.Cancelled)]
    public void Retry_From_Invalid_Status_Should_Throw(RenderJobStatus targetStatus)
    {
        var job = CreateTestJob();
        
        if (targetStatus == RenderJobStatus.Completed)
        {
            job.Queue();
            job.Start();
            job.Complete();
        }
        else
        {
            job.Cancel("user");
        }
        
        Action act = () => job.Retry("admin-1");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_From_Pending_Queued_Processing_Should_Succeed()
    {
        var job1 = CreateTestJob();
        job1.Cancel("user-1");
        job1.Status.Should().Be(RenderJobStatus.Cancelled);

        var job2 = CreateTestJob();
        job2.Queue();
        job2.Cancel("user-1");
        job2.Status.Should().Be(RenderJobStatus.Cancelled);

        var job3 = CreateTestJob();
        job3.Queue();
        job3.Start();
        job3.Cancel("user-1");
        job3.Status.Should().Be(RenderJobStatus.Cancelled);
    }
}
