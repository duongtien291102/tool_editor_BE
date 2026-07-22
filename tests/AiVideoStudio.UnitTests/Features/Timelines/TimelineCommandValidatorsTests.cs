using System;
using AiVideoStudio.Application.Features.Timelines;
using AiVideoStudio.Application.Features.Timelines.DTOs;
using AiVideoStudio.Application.Features.Timelines.Validators;
using AiVideoStudio.Domain.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Timelines;

public class TimelineCommandValidatorsTests
{
    private readonly CreateTimelineCommandValidator _createValidator = new();
    private readonly UpdateTimelineCommandValidator _updateValidator = new();
    private readonly DeleteTimelineCommandValidator _deleteValidator = new();
    private readonly AutoSaveTimelineCommandValidator _autoSaveValidator = new();
    private readonly AddTrackCommandValidator _addTrackValidator = new();
    private readonly RemoveTrackCommandValidator _removeTrackValidator = new();
    private readonly ReorderTrackCommandValidator _reorderTrackValidator = new();
    private readonly UpdateTrackCommandValidator _updateTrackValidator = new();
    private readonly AddClipCommandValidator _addClipValidator = new();
    private readonly UpdateClipCommandValidator _updateClipValidator = new();
    private readonly MoveClipCommandValidator _moveClipValidator = new();
    private readonly ResizeClipCommandValidator _resizeClipValidator = new();
    private readonly DeleteClipCommandValidator _deleteClipValidator = new();
    private readonly GetTimelineByProjectQueryValidator _getByProjectValidator = new();
    private readonly GetTimelineQueryValidator _getByIdValidator = new();

    [Fact]
    public void CreateTimelineCommandValidator_Should_Pass_For_Valid_Command()
    {
        var cmd = new CreateTimelineCommand("p1", "Main Timeline", 30.0, 1920, 1080);
        var result = _createValidator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Main Timeline", 30.0, 1920, 1080)]
    [InlineData(null, "Main Timeline", 30.0, 1920, 1080)]
    [InlineData("p1", "", 30.0, 1920, 1080)]
    [InlineData("p1", null, 30.0, 1920, 1080)]
    [InlineData("p1", "Main Timeline", 0, 1920, 1080)]
    [InlineData("p1", "Main Timeline", -1.0, 1920, 1080)]
    [InlineData("p1", "Main Timeline", 30.0, 0, 1080)]
    [InlineData("p1", "Main Timeline", 30.0, 1920, 0)]
    public void CreateTimelineCommandValidator_Should_Fail_For_Invalid_Command(
        string? projectId, string? name, double frameRate, int width, int height)
    {
        var cmd = new CreateTimelineCommand(projectId!, name!, frameRate, width, height);
        var result = _createValidator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void CreateTimelineCommandValidator_Should_Fail_When_Name_Exceeds_MaxLength()
    {
        var longName = new string('a', 201);
        var cmd = new CreateTimelineCommand("p1", longName, 30.0, 1920, 1080);
        var result = _createValidator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void UpdateTimelineCommandValidator_Should_Pass_For_Valid_Command()
    {
        var cmd = new UpdateTimelineCommand("t1", "Updated Timeline", 60.0, 3840, 2160);
        var result = _updateValidator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Updated", 30.0, 1920, 1080)]
    [InlineData(null, "Updated", 30.0, 1920, 1080)]
    [InlineData("t1", "", 30.0, 1920, 1080)]
    [InlineData("t1", "Updated", 0, 1920, 1080)]
    [InlineData("t1", "Updated", 30.0, 0, 1080)]
    [InlineData("t1", "Updated", 30.0, 1920, 0)]
    public void UpdateTimelineCommandValidator_Should_Fail_For_Invalid_Command(
        string? id, string? name, double frameRate, int width, int height)
    {
        var cmd = new UpdateTimelineCommand(id!, name!, frameRate, width, height);
        var result = _updateValidator.TestValidate(cmd);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void DeleteTimelineCommandValidator_Validation_Tests()
    {
        _deleteValidator.TestValidate(new DeleteTimelineCommand("t1")).ShouldNotHaveAnyValidationErrors();
        _deleteValidator.TestValidate(new DeleteTimelineCommand("")).ShouldHaveValidationErrorFor(x => x.Id);
        _deleteValidator.TestValidate(new DeleteTimelineCommand(null!)).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void AutoSaveTimelineCommandValidator_Validation_Tests()
    {
        var dto = new TimelineDto("t1", "p1", "u1", "T", 1, 30, 1920, 1080, TimeSpan.Zero, DateTimeOffset.UtcNow, null, new System.Collections.Generic.List<TrackDto>());
        _autoSaveValidator.TestValidate(new AutoSaveTimelineCommand("t1", dto)).ShouldNotHaveAnyValidationErrors();
        _autoSaveValidator.TestValidate(new AutoSaveTimelineCommand("", dto)).ShouldHaveValidationErrorFor(x => x.Id);
        _autoSaveValidator.TestValidate(new AutoSaveTimelineCommand("t1", null!)).ShouldHaveValidationErrorFor(x => x.Data);
    }

    [Fact]
    public void AddTrackCommandValidator_Validation_Tests()
    {
        _addTrackValidator.TestValidate(new AddTrackCommand("t1", "V1", TrackType.Video)).ShouldNotHaveAnyValidationErrors();
        _addTrackValidator.TestValidate(new AddTrackCommand("", "V1", TrackType.Video)).ShouldHaveValidationErrorFor(x => x.TimelineId);
        _addTrackValidator.TestValidate(new AddTrackCommand("t1", "", TrackType.Video)).ShouldHaveValidationErrorFor(x => x.Name);
        _addTrackValidator.TestValidate(new AddTrackCommand("t1", new string('x', 201), TrackType.Video)).ShouldHaveValidationErrorFor(x => x.Name);
        _addTrackValidator.TestValidate(new AddTrackCommand("t1", "V1", (TrackType)999)).ShouldHaveValidationErrorFor(x => x.TrackType);
    }

    [Fact]
    public void RemoveTrackCommandValidator_Validation_Tests()
    {
        _removeTrackValidator.TestValidate(new RemoveTrackCommand("t1", "tr1")).ShouldNotHaveAnyValidationErrors();
        _removeTrackValidator.TestValidate(new RemoveTrackCommand("", "tr1")).ShouldHaveValidationErrorFor(x => x.TimelineId);
        _removeTrackValidator.TestValidate(new RemoveTrackCommand("t1", "")).ShouldHaveValidationErrorFor(x => x.TrackId);
    }

    [Fact]
    public void ReorderTrackCommandValidator_Validation_Tests()
    {
        _reorderTrackValidator.TestValidate(new ReorderTrackCommand("t1", "tr1", 0)).ShouldNotHaveAnyValidationErrors();
        _reorderTrackValidator.TestValidate(new ReorderTrackCommand("t1", "tr1", 5)).ShouldNotHaveAnyValidationErrors();
        _reorderTrackValidator.TestValidate(new ReorderTrackCommand("", "tr1", 0)).ShouldHaveValidationErrorFor(x => x.TimelineId);
        _reorderTrackValidator.TestValidate(new ReorderTrackCommand("t1", "", 0)).ShouldHaveValidationErrorFor(x => x.TrackId);
        _reorderTrackValidator.TestValidate(new ReorderTrackCommand("t1", "tr1", -1)).ShouldHaveValidationErrorFor(x => x.NewOrder);
    }

    [Fact]
    public void UpdateTrackCommandValidator_Validation_Tests()
    {
        _updateTrackValidator.TestValidate(new UpdateTrackCommand("t1", "tr1", "Name", false, false, false)).ShouldNotHaveAnyValidationErrors();
        _updateTrackValidator.TestValidate(new UpdateTrackCommand("", "tr1", "Name", false, false, false)).ShouldHaveValidationErrorFor(x => x.TimelineId);
        _updateTrackValidator.TestValidate(new UpdateTrackCommand("t1", "", "Name", false, false, false)).ShouldHaveValidationErrorFor(x => x.TrackId);
        _updateTrackValidator.TestValidate(new UpdateTrackCommand("t1", "tr1", "", false, false, false)).ShouldHaveValidationErrorFor(x => x.Name);
        _updateTrackValidator.TestValidate(new UpdateTrackCommand("t1", "tr1", new string('a', 201), false, false, false)).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void AddClipCommandValidator_Validation_Tests()
    {
        _addClipValidator.TestValidate(new AddClipCommand("t1", "tr1", "a1", TimeSpan.Zero, TimeSpan.FromSeconds(5), "Clip", null, null)).ShouldNotHaveAnyValidationErrors();
        _addClipValidator.TestValidate(new AddClipCommand("", "tr1", "a1", TimeSpan.Zero, TimeSpan.FromSeconds(5), "Clip", null, null)).ShouldHaveValidationErrorFor(x => x.TimelineId);
        _addClipValidator.TestValidate(new AddClipCommand("t1", "", "a1", TimeSpan.Zero, TimeSpan.FromSeconds(5), "Clip", null, null)).ShouldHaveValidationErrorFor(x => x.TrackId);
        _addClipValidator.TestValidate(new AddClipCommand("t1", "tr1", "", TimeSpan.Zero, TimeSpan.FromSeconds(5), "Clip", null, null)).ShouldHaveValidationErrorFor(x => x.AssetId);
        _addClipValidator.TestValidate(new AddClipCommand("t1", "tr1", "a1", TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(5), "Clip", null, null)).ShouldHaveValidationErrorFor(x => x.StartFrame);
        _addClipValidator.TestValidate(new AddClipCommand("t1", "tr1", "a1", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), "Clip", null, null)).ShouldHaveValidationErrorFor(x => x.EndFrame);
        _addClipValidator.TestValidate(new AddClipCommand("t1", "tr1", "a1", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(4), "Clip", null, null)).ShouldHaveValidationErrorFor(x => x.EndFrame);
    }

    [Fact]
    public void UpdateClipCommandValidator_Validation_Tests()
    {
        _updateClipValidator.TestValidate(new UpdateClipCommand("t1", "c1", "Clip", 0, 1.0, TimeSpan.Zero, TimeSpan.Zero, 1.0, null)).ShouldNotHaveAnyValidationErrors();
        _updateClipValidator.TestValidate(new UpdateClipCommand("", "c1", "Clip", 0, 1.0, TimeSpan.Zero, TimeSpan.Zero, 1.0, null)).ShouldHaveValidationErrorFor(x => x.TimelineId);
        _updateClipValidator.TestValidate(new UpdateClipCommand("t1", "", "Clip", 0, 1.0, TimeSpan.Zero, TimeSpan.Zero, 1.0, null)).ShouldHaveValidationErrorFor(x => x.ClipId);
    }

    [Fact]
    public void MoveClipCommandValidator_Validation_Tests()
    {
        _moveClipValidator.TestValidate(new MoveClipCommand("t1", "c1", "tr2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))).ShouldNotHaveAnyValidationErrors();
        _moveClipValidator.TestValidate(new MoveClipCommand("", "c1", "tr2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))).ShouldHaveValidationErrorFor(x => x.TimelineId);
        _moveClipValidator.TestValidate(new MoveClipCommand("t1", "", "tr2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))).ShouldHaveValidationErrorFor(x => x.ClipId);
        _moveClipValidator.TestValidate(new MoveClipCommand("t1", "c1", "", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))).ShouldHaveValidationErrorFor(x => x.NewTrackId);
        _moveClipValidator.TestValidate(new MoveClipCommand("t1", "c1", "tr2", TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(10))).ShouldHaveValidationErrorFor(x => x.NewStartFrame);
        _moveClipValidator.TestValidate(new MoveClipCommand("t1", "c1", "tr2", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5))).ShouldHaveValidationErrorFor(x => x.NewEndFrame);
    }

    [Fact]
    public void ResizeClipCommandValidator_Validation_Tests()
    {
        _resizeClipValidator.TestValidate(new ResizeClipCommand("t1", "c1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10))).ShouldNotHaveAnyValidationErrors();
        _resizeClipValidator.TestValidate(new ResizeClipCommand("", "c1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10))).ShouldHaveValidationErrorFor(x => x.TimelineId);
        _resizeClipValidator.TestValidate(new ResizeClipCommand("t1", "", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10))).ShouldHaveValidationErrorFor(x => x.ClipId);
        _resizeClipValidator.TestValidate(new ResizeClipCommand("t1", "c1", TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(10))).ShouldHaveValidationErrorFor(x => x.NewStartFrame);
        _resizeClipValidator.TestValidate(new ResizeClipCommand("t1", "c1", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5))).ShouldHaveValidationErrorFor(x => x.NewEndFrame);
    }

    [Fact]
    public void DeleteClipCommandValidator_Validation_Tests()
    {
        _deleteClipValidator.TestValidate(new DeleteClipCommand("t1", "c1")).ShouldNotHaveAnyValidationErrors();
        _deleteClipValidator.TestValidate(new DeleteClipCommand("", "c1")).ShouldHaveValidationErrorFor(x => x.TimelineId);
        _deleteClipValidator.TestValidate(new DeleteClipCommand("t1", "")).ShouldHaveValidationErrorFor(x => x.ClipId);
    }

    [Fact]
    public void QueryValidators_Validation_Tests()
    {
        _getByProjectValidator.TestValidate(new GetTimelineByProjectQuery("p1")).ShouldNotHaveAnyValidationErrors();
        _getByProjectValidator.TestValidate(new GetTimelineByProjectQuery("")).ShouldHaveValidationErrorFor(x => x.ProjectId);

        _getByIdValidator.TestValidate(new GetTimelineQuery("t1")).ShouldNotHaveAnyValidationErrors();
        _getByIdValidator.TestValidate(new GetTimelineQuery("")).ShouldHaveValidationErrorFor(x => x.Id);
    }
}
