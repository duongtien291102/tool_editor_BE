using System;
using System.Linq;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace AiVideoStudio.UnitTests.DomainTests;

public class TimelineTests
{
    [Fact]
    public void CreateTimeline_Should_Initialize_Correctly()
    {
        var timeline = Timeline.Create("project-1", "user-1", "Main Timeline");
        
        timeline.ProjectId.Should().Be("project-1");
        timeline.Name.Should().Be("Main Timeline");
        timeline.Version.Should().Be(1);
        timeline.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CreateTimeline_ShouldInitializeVersionToOne()
    {
        var timeline = Timeline.Create("p1", "u1", "Timeline 1");
        timeline.Version.Should().Be(1);
    }

    [Fact]
    public void CreateTimeline_ShouldHaveNoTracks()
    {
        var timeline = Timeline.Create("p1", "u1", "Timeline 1");
        timeline.Tracks.Should().BeEmpty();
    }

    [Fact]
    public void AddTrack_ShouldIncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var initialVersion = timeline.Version;
        timeline.AddTrack("V1", TrackType.Video, "u1");
        timeline.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void AddTrack_Should_Increase_Version_And_Normalize_Order()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track1 = timeline.AddTrack("V1", TrackType.Video, "u1");
        var track2 = timeline.AddTrack("V2", TrackType.Video, "u1");

        timeline.Version.Should().Be(3);
        track1.Order.Should().Be(0);
        track2.Order.Should().Be(1);
    }

    [Fact]
    public void RemoveTrack_ShouldIncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("V1", TrackType.Video, "u1");
        var versionBeforeRemove = timeline.Version;

        timeline.RemoveTrack(track.Id, "u1");
        timeline.Version.Should().Be(versionBeforeRemove + 1);
        timeline.Tracks.Should().BeEmpty();
    }

    [Fact]
    public void ReorderTrack_ShouldIncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var t1 = timeline.AddTrack("V1", TrackType.Video, "u1");
        var t2 = timeline.AddTrack("V2", TrackType.Video, "u1");
        var versionBefore = timeline.Version;

        timeline.ReorderTrack(t2.Id, 0, "u1");
        timeline.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void ReorderTrack_Should_Normalize_Continuously()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var t1 = timeline.AddTrack("V1", TrackType.Video, "u1");
        var t2 = timeline.AddTrack("V2", TrackType.Video, "u1");
        var t3 = timeline.AddTrack("V3", TrackType.Video, "u1");

        timeline.ReorderTrack(t3.Id, 0, "u1");

        t3.Order.Should().Be(0);
        t1.Order.Should().Be(1);
        t2.Order.Should().Be(2);
    }

    [Fact]
    public void TrackOrder_ShouldRemainContinuous_AfterMultipleOperations()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var t1 = timeline.AddTrack("V1", TrackType.Video, "u1");
        var t2 = timeline.AddTrack("V2", TrackType.Video, "u1");
        var t3 = timeline.AddTrack("A1", TrackType.Audio, "u1");
        var t4 = timeline.AddTrack("O1", TrackType.Overlay, "u1");

        timeline.RemoveTrack(t2.Id, "u1");
        timeline.ReorderTrack(t4.Id, 0, "u1");

        var ordered = timeline.Tracks.OrderBy(t => t.Order).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Order.Should().Be(i);
        }
    }

    [Fact]
    public void AddClip_ShouldIncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("V1", TrackType.Video, "u1");
        var versionBefore = timeline.Version;

        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), "Clip 1", null, null, "u1");
        timeline.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void MoveClip_ShouldIncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var t1 = timeline.AddTrack("V1", TrackType.Video, "u1");
        var t2 = timeline.AddTrack("V2", TrackType.Video, "u1");
        var clip = timeline.AddClip(t1.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), "Clip 1", null, null, "u1");
        var versionBefore = timeline.Version;

        timeline.MoveClip(clip.Id, t2.Id, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), "u1");
        timeline.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void ResizeClip_ShouldIncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("V1", TrackType.Video, "u1");
        var clip = timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), "Clip 1", null, null, "u1");
        var versionBefore = timeline.Version;

        timeline.ResizeClip(clip.Id, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(8), "u1");
        timeline.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void DeleteClip_ShouldIncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("V1", TrackType.Video, "u1");
        var clip = timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), "Clip 1", null, null, "u1");
        var versionBefore = timeline.Version;

        timeline.RemoveClip(clip.Id, "u1");
        timeline.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void TimelineDuration_ShouldDecrease_WhenLastClipDeleted()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("V1", TrackType.Video, "u1");
        var clip1 = timeline.AddClip(track.Id, "a1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), "c1", null, null, "u1");
        var clip2 = timeline.AddClip(track.Id, "a2", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), "c2", null, null, "u1");

        timeline.Duration.Should().Be(TimeSpan.FromSeconds(20));

        timeline.RemoveClip(clip2.Id, "u1");
        timeline.Duration.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddClip_To_VideoTrack_With_Overlap_Should_Throw()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("V1", TrackType.Video, "u1");

        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "Clip 1", null, null, "u1");
        
        Action act = () => timeline.AddClip(track.Id, "asset2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), "Clip 2", null, null, "u1");
        act.Should().Throw<InvalidOperationException>().WithMessage("ClipOverlap");
    }

    [Fact]
    public void AddClip_To_AudioTrack_With_Overlap_Should_Throw()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("A1", TrackType.Audio, "u1");

        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "Clip 1", null, null, "u1");
        
        Action act = () => timeline.AddClip(track.Id, "asset2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), "Clip 2", null, null, "u1");
        act.Should().Throw<InvalidOperationException>().WithMessage("ClipOverlap");
    }

    [Fact]
    public void AddClip_To_OverlayTrack_With_Overlap_Should_Succeed()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("O1", TrackType.Overlay, "u1");

        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "Clip 1", null, null, "u1");
        var clip2 = timeline.AddClip(track.Id, "asset2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), "Clip 2", null, null, "u1");
        
        clip2.StartFrame.Should().Be(TimeSpan.FromSeconds(5));
        timeline.Duration.Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void AddClip_To_SubtitleTrack_With_Overlap_Should_Succeed()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("S1", TrackType.Subtitle, "u1");

        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "Clip 1", null, null, "u1");
        var clip2 = timeline.AddClip(track.Id, "asset2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), "Clip 2", null, null, "u1");
        
        clip2.StartFrame.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddClip_To_EffectTrack_With_Overlap_Should_Succeed()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("E1", TrackType.Effect, "u1");

        timeline.AddClip(track.Id, "asset1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "Clip 1", null, null, "u1");
        var clip2 = timeline.AddClip(track.Id, "asset2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), "Clip 2", null, null, "u1");
        
        clip2.StartFrame.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Reject_DeleteTrack_WhenContainsClips()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("V1", TrackType.Video, "u1");
        timeline.AddClip(track.Id, "a1", TimeSpan.Zero, TimeSpan.FromSeconds(10), "c1", null, null, "u1");

        Action act = () => timeline.RemoveTrack(track.Id, "u1");
        act.Should().Throw<InvalidOperationException>().WithMessage("TrackContainsClips");
    }

    [Fact]
    public void Reject_StartFrame_LessThanZero()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("V1", TrackType.Video, "u1");

        Action act = () => timeline.AddClip(track.Id, "a1", TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(10), "c1", null, null, "u1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reject_EndFrame_LessThanOrEqualStartFrame()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var track = timeline.AddTrack("V1", TrackType.Video, "u1");

        Action act = () => timeline.AddClip(track.Id, "a1", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), "c1", null, null, "u1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SoftDelete_Should_Set_DeletedAt_And_IncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var initialVersion = timeline.Version;

        timeline.SoftDelete("u1");

        timeline.IsDeleted.Should().BeTrue();
        timeline.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void ForceIncrementVersionForAutoSave_ShouldIncrementVersion()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var initialVersion = timeline.Version;

        timeline.ForceIncrementVersionForAutoSave("u1");
        timeline.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void MoveClip_To_Another_Track_Should_Update_TrackId()
    {
        var timeline = Timeline.Create("p1", "u1", "t");
        var t1 = timeline.AddTrack("V1", TrackType.Video, "u1");
        var t2 = timeline.AddTrack("V2", TrackType.Video, "u1");
        
        var clip = timeline.AddClip(t1.Id, "a1", TimeSpan.Zero, TimeSpan.FromSeconds(10), "c1", null, null, "u1");
        
        timeline.MoveClip(clip.Id, t2.Id, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), "u1");
        
        t1.Clips.Should().BeEmpty();
        t2.Clips.Should().ContainSingle().Which.Id.Should().Be(clip.Id);
        timeline.Duration.Should().Be(TimeSpan.FromSeconds(15));
    }
}
