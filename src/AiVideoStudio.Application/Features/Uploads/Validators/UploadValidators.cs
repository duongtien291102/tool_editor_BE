using FluentValidation;
namespace AiVideoStudio.Application.Features.Uploads.Validators;
public sealed class StartUploadValidator : AbstractValidator<StartUploadCommand>
{
    public StartUploadValidator() { RuleFor(x=>x.ProjectId).NotEmpty(); RuleFor(x=>x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x=>x.ContentType).NotEmpty(); RuleFor(x=>x.FileSize).GreaterThan(0); RuleFor(x=>x.ChunkCount).InclusiveBetween(1,10000);
        RuleFor(x=>x.Checksum).Matches("^[a-fA-F0-9]{64}$"); }
}
public sealed class UploadChunkValidator : AbstractValidator<UploadChunkCommand>
{
    public UploadChunkValidator() { RuleFor(x=>x.UploadId).NotEmpty(); RuleFor(x=>x.ChunkIndex).GreaterThanOrEqualTo(0);
        RuleFor(x=>x.Data).NotNull().Must(x=>x.Length>0); RuleFor(x=>x.Checksum).Matches("^[a-fA-F0-9]{64}$"); }
}
public sealed class CompleteUploadValidator : AbstractValidator<CompleteUploadCommand> { public CompleteUploadValidator()=>RuleFor(x=>x.UploadId).NotEmpty(); }
public sealed class CancelUploadValidator : AbstractValidator<CancelUploadCommand> { public CancelUploadValidator()=>RuleFor(x=>x.UploadId).NotEmpty(); }
public sealed class RetryUploadValidator : AbstractValidator<RetryUploadCommand> { public RetryUploadValidator()=>RuleFor(x=>x.UploadId).NotEmpty(); }
