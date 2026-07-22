using System;
using AiVideoStudio.Application.Features.Timelines.DTOs;
using FluentValidation;

namespace AiVideoStudio.Application.Features.Timelines.Validators;

public class CreateTimelineCommandValidator : AbstractValidator<CreateTimelineCommand>
{
    public CreateTimelineCommandValidator()
    {
        RuleFor(v => v.ProjectId).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
        RuleFor(v => v.FrameRate).GreaterThan(0);
        RuleFor(v => v.ResolutionWidth).GreaterThan(0);
        RuleFor(v => v.ResolutionHeight).GreaterThan(0);
    }
}

public class UpdateTimelineCommandValidator : AbstractValidator<UpdateTimelineCommand>
{
    public UpdateTimelineCommandValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
        RuleFor(v => v.FrameRate).GreaterThan(0);
        RuleFor(v => v.ResolutionWidth).GreaterThan(0);
        RuleFor(v => v.ResolutionHeight).GreaterThan(0);
    }
}

public class DeleteTimelineCommandValidator : AbstractValidator<DeleteTimelineCommand>
{
    public DeleteTimelineCommandValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
    }
}

public class AutoSaveTimelineCommandValidator : AbstractValidator<AutoSaveTimelineCommand>
{
    public AutoSaveTimelineCommandValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
        RuleFor(v => v.Data).NotNull();
    }
}

public class AddTrackCommandValidator : AbstractValidator<AddTrackCommand>
{
    public AddTrackCommandValidator()
    {
        RuleFor(v => v.TimelineId).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
        RuleFor(v => v.TrackType).IsInEnum();
    }
}

public class RemoveTrackCommandValidator : AbstractValidator<RemoveTrackCommand>
{
    public RemoveTrackCommandValidator()
    {
        RuleFor(v => v.TimelineId).NotEmpty();
        RuleFor(v => v.TrackId).NotEmpty();
    }
}

public class ReorderTrackCommandValidator : AbstractValidator<ReorderTrackCommand>
{
    public ReorderTrackCommandValidator()
    {
        RuleFor(v => v.TimelineId).NotEmpty();
        RuleFor(v => v.TrackId).NotEmpty();
        RuleFor(v => v.NewOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateTrackCommandValidator : AbstractValidator<UpdateTrackCommand>
{
    public UpdateTrackCommandValidator()
    {
        RuleFor(v => v.TimelineId).NotEmpty();
        RuleFor(v => v.TrackId).NotEmpty();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
    }
}

public class AddClipCommandValidator : AbstractValidator<AddClipCommand>
{
    public AddClipCommandValidator()
    {
        RuleFor(v => v.TimelineId).NotEmpty();
        RuleFor(v => v.TrackId).NotEmpty();
        RuleFor(v => v.AssetId).NotEmpty();
        RuleFor(v => v.StartFrame).GreaterThanOrEqualTo(TimeSpan.Zero);
        RuleFor(v => v.EndFrame).GreaterThan(v => v.StartFrame);
    }
}

public class UpdateClipCommandValidator : AbstractValidator<UpdateClipCommand>
{
    public UpdateClipCommandValidator()
    {
        RuleFor(v => v.TimelineId).NotEmpty();
        RuleFor(v => v.ClipId).NotEmpty();
    }
}

public class MoveClipCommandValidator : AbstractValidator<MoveClipCommand>
{
    public MoveClipCommandValidator()
    {
        RuleFor(v => v.TimelineId).NotEmpty();
        RuleFor(v => v.ClipId).NotEmpty();
        RuleFor(v => v.NewTrackId).NotEmpty();
        RuleFor(v => v.NewStartFrame).GreaterThanOrEqualTo(TimeSpan.Zero);
        RuleFor(v => v.NewEndFrame).GreaterThan(v => v.NewStartFrame);
    }
}

public class ResizeClipCommandValidator : AbstractValidator<ResizeClipCommand>
{
    public ResizeClipCommandValidator()
    {
        RuleFor(v => v.TimelineId).NotEmpty();
        RuleFor(v => v.ClipId).NotEmpty();
        RuleFor(v => v.NewStartFrame).GreaterThanOrEqualTo(TimeSpan.Zero);
        RuleFor(v => v.NewEndFrame).GreaterThan(v => v.NewStartFrame);
    }
}

public class DeleteClipCommandValidator : AbstractValidator<DeleteClipCommand>
{
    public DeleteClipCommandValidator()
    {
        RuleFor(v => v.TimelineId).NotEmpty();
        RuleFor(v => v.ClipId).NotEmpty();
    }
}

public class GetTimelineByProjectQueryValidator : AbstractValidator<GetTimelineByProjectQuery>
{
    public GetTimelineByProjectQueryValidator()
    {
        RuleFor(v => v.ProjectId).NotEmpty();
    }
}

public class GetTimelineQueryValidator : AbstractValidator<GetTimelineQuery>
{
    public GetTimelineQueryValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
    }
}
