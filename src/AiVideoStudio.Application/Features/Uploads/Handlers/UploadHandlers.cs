using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Features.Uploads.DTOs;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Storage;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AiVideoStudio.Shared.Responses;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Options;

namespace AiVideoStudio.Application.Features.Uploads.Handlers;

public sealed class UploadCommandHandlers : IRequestHandler<StartUploadCommand,Result<UploadSessionDto>>,
    IRequestHandler<UploadChunkCommand,Result<ChunkDto>>, IRequestHandler<CompleteUploadCommand,Result<UploadSessionDto>>,
    IRequestHandler<CancelUploadCommand,Result>, IRequestHandler<RetryUploadCommand,Result<UploadSessionDto>>
{
    private readonly IUploadSessionRepository _uploads; private readonly IProjectRepository _projects; private readonly IMediaAssetRepository _assets;
    private readonly IChunkUploadEngine _chunks; private readonly IStorageProvider _storage; private readonly IThumbnailGenerator _thumbnails;
    private readonly IMetadataExtractor _metadata; private readonly IAssetManifestBuilder _manifests; private readonly ICurrentUser _user;
    private readonly IMapper _mapper; private readonly StorageOptions _options;
    public UploadCommandHandlers(IUploadSessionRepository uploads,IProjectRepository projects,IMediaAssetRepository assets,IChunkUploadEngine chunks,
        IStorageProvider storage,IThumbnailGenerator thumbnails,IMetadataExtractor metadata,IAssetManifestBuilder manifests,ICurrentUser user,IMapper mapper,IOptions<StorageOptions> options)
    { _uploads=uploads;_projects=projects;_assets=assets;_chunks=chunks;_storage=storage;_thumbnails=thumbnails;_metadata=metadata;_manifests=manifests;_user=user;_mapper=mapper;_options=options.Value; }

    public async Task<Result<UploadSessionDto>> Handle(StartUploadCommand r,CancellationToken ct)
    {
        if(!Authenticated()) return Result<UploadSessionDto>.Failure(UploadErrors.Unauthorized);
        var project=await _projects.GetByIdAsync(r.ProjectId,ct); if(project is null) return Result<UploadSessionDto>.Failure(UploadErrors.ProjectNotFound);
        if(!Access(project.OwnerId)) return Result<UploadSessionDto>.Failure(UploadErrors.Forbidden);
        var ext=Path.GetExtension(r.FileName); if(r.FileSize>_options.MaxFileSizeBytes || !_options.AllowedExtensions.Contains(ext,StringComparer.OrdinalIgnoreCase) || !_options.AllowedMimeTypes.Contains(r.ContentType,StringComparer.OrdinalIgnoreCase))
            return Result<UploadSessionDto>.Failure(UploadErrors.InvalidState);
        var session=UploadSession.Create(r.ProjectId,_user.UserId!,r.FileName,r.ContentType,r.FileSize,r.ChunkCount,r.Checksum);
        await _uploads.AddAsync(session,ct); return Result<UploadSessionDto>.Success(_mapper.Map<UploadSessionDto>(session));
    }
    public async Task<Result<ChunkDto>> Handle(UploadChunkCommand r,CancellationToken ct)
    {
        var session=await _uploads.GetByIdAsync(r.UploadId,ct); var error=Guard<ChunkDto>(session); if(error is not null)return error;
        if(session!.HasChunk(r.ChunkIndex)) return Result<ChunkDto>.Success(new ChunkDto(r.ChunkIndex,0,r.Checksum,true));
        try { var chunk=await _chunks.StoreChunkAsync(session,r.ChunkIndex,r.Data,r.Checksum,ct); session.UploadChunk(r.ChunkIndex,r.Data.LongLength); await _uploads.UpdateAsync(session,ct); return Result<ChunkDto>.Success(chunk); }
        catch(InvalidDataException){return Result<ChunkDto>.Failure(UploadErrors.ChecksumMismatch);} catch(Exception){return Result<ChunkDto>.Failure(UploadErrors.InvalidChunk);}
    }
    public async Task<Result<UploadSessionDto>> Handle(CompleteUploadCommand r,CancellationToken ct)
    {
        var session=await _uploads.GetByIdAsync(r.UploadId,ct); var error=Guard<UploadSessionDto>(session); if(error is not null)return error;
        try
        {
            session!.Merge(); await _uploads.UpdateAsync(session,ct); var storagePath=await _chunks.MergeAsync(session,ct);
            await using var stream=await _storage.OpenReadStreamAsync(string.Empty,storagePath,ct); var kind=ResolveKind(session.ContentType,session.FileName);
            var meta=kind switch { AssetType.Image=>await _metadata.ExtractImageAsync(stream,ct),AssetType.Video=>await _metadata.ExtractVideoAsync(stream,ct),
                AssetType.Audio=>await _metadata.ExtractAudioAsync(stream,ct),AssetType.Subtitle=>await _metadata.ExtractSubtitleAsync(stream,ct),
                _=>new AssetMetadataDto(null,null,null,"other",new Dictionary<string,string>())};
            string? thumb=kind switch { AssetType.Image=>await _thumbnails.GenerateImageThumbnailAsync(session.ProjectId,session.AssetId,storagePath,ct),
                AssetType.Video=>await _thumbnails.GenerateVideoThumbnailAsync(session.ProjectId,session.AssetId,storagePath,ct),
                AssetType.Audio=>await _thumbnails.GenerateAudioWaveformAsync(session.ProjectId,session.AssetId,storagePath,ct),_=>null};
            var manifest=_manifests.Build(session,storagePath,thumb,meta); var manifestPath=await _manifests.SaveAsync(session.ProjectId,manifest,ct);
            var asset=MediaAsset.Create(session.ProjectId,session.OwnerId,Path.GetFileName(storagePath),session.FileName,Path.GetExtension(session.FileName),session.ContentType,
                session.FileSize,storagePath,kind,meta.Width,meta.Height,meta.Duration,thumb); await _assets.AddAsync(asset,ct);
            session.Complete(storagePath,manifestPath); await _uploads.UpdateAsync(session,ct); await _chunks.DeleteChunksAsync(session.Id,ct);
            return Result<UploadSessionDto>.Success(_mapper.Map<UploadSessionDto>(session));
        }
        catch(InvalidDataException){session!.Fail("Checksum mismatch.");await _uploads.UpdateAsync(session,ct);return Result<UploadSessionDto>.Failure(UploadErrors.ChecksumMismatch);}
        catch(InvalidOperationException){return Result<UploadSessionDto>.Failure(UploadErrors.InvalidState);}
        catch(Exception){if(session!.Status!=UploadStatus.Failed){session.Fail("Storage processing failed.");await _uploads.UpdateAsync(session,ct);}return Result<UploadSessionDto>.Failure(UploadErrors.StorageFailure);}
    }
    public async Task<Result> Handle(CancelUploadCommand r,CancellationToken ct){var s=await _uploads.GetByIdAsync(r.UploadId,ct);var e=Guard<object>(s);if(e is not null)return Result.Failure(e.Error);try{s!.Cancel();await _uploads.UpdateAsync(s,ct);await _chunks.DeleteChunksAsync(s.Id,ct);return Result.Success();}catch{return Result.Failure(UploadErrors.InvalidState);}}
    public async Task<Result<UploadSessionDto>> Handle(RetryUploadCommand r,CancellationToken ct){var s=await _uploads.GetByIdAsync(r.UploadId,ct);var e=Guard<UploadSessionDto>(s);if(e is not null)return e;try{s!.Retry();await _uploads.UpdateAsync(s,ct);return Result<UploadSessionDto>.Success(_mapper.Map<UploadSessionDto>(s));}catch{return Result<UploadSessionDto>.Failure(UploadErrors.InvalidState);}}
    private Result<T>? Guard<T>(UploadSession? s){if(s is null)return Result<T>.Failure(UploadErrors.NotFound);if(!Authenticated())return Result<T>.Failure(UploadErrors.Unauthorized);if(!Access(s.OwnerId))return Result<T>.Failure(UploadErrors.Forbidden);return null;}
    private bool Authenticated()=>_user.IsAuthenticated&&!string.IsNullOrWhiteSpace(_user.UserId); private bool Access(string owner)=>owner==_user.UserId||_user.Roles.Contains("Admin")||_user.Roles.Contains("Administrator");
    private static AssetType ResolveKind(string mime,string file)=>mime.StartsWith("image/")?AssetType.Image:mime.StartsWith("video/")?AssetType.Video:mime.StartsWith("audio/")?AssetType.Audio:Path.GetExtension(file).ToLowerInvariant() is ".srt" or ".vtt"?AssetType.Subtitle:AssetType.Other;
}

public sealed class UploadQueryHandlers : IRequestHandler<GetUploadSessionQuery,Result<UploadSessionDto>>, IRequestHandler<GetProjectUploadsQuery,Result<PagedResult<UploadSummaryDto>>>
{
    private readonly IUploadSessionRepository _repo;private readonly IProjectRepository _projects;private readonly ICurrentUser _user;private readonly IMapper _mapper;
    public UploadQueryHandlers(IUploadSessionRepository r,IProjectRepository p,ICurrentUser u,IMapper m){_repo=r;_projects=p;_user=u;_mapper=m;}
    public async Task<Result<UploadSessionDto>> Handle(GetUploadSessionQuery q,CancellationToken ct){var s=await _repo.GetByIdAsync(q.Id,ct);if(s is null)return Result<UploadSessionDto>.Failure(UploadErrors.NotFound);if(!_user.IsAuthenticated)return Result<UploadSessionDto>.Failure(UploadErrors.Unauthorized);if(!Access(s.OwnerId))return Result<UploadSessionDto>.Failure(UploadErrors.Forbidden);return Result<UploadSessionDto>.Success(_mapper.Map<UploadSessionDto>(s));}
    public async Task<Result<PagedResult<UploadSummaryDto>>> Handle(GetProjectUploadsQuery q,CancellationToken ct){if(!_user.IsAuthenticated)return Result<PagedResult<UploadSummaryDto>>.Failure(UploadErrors.Unauthorized);var p=await _projects.GetByIdAsync(q.ProjectId,ct);if(p is null)return Result<PagedResult<UploadSummaryDto>>.Failure(UploadErrors.ProjectNotFound);if(!Access(p.OwnerId))return Result<PagedResult<UploadSummaryDto>>.Failure(UploadErrors.Forbidden);var page=await _repo.GetByProjectIdPagedAsync(q.ProjectId,q.Page,q.PageSize,ct);var items=page.Items.Select(_mapper.Map<UploadSummaryDto>).ToList();return Result<PagedResult<UploadSummaryDto>>.Success(new(items,page.TotalCount,page.Page,page.PageSize));}
    private bool Access(string o)=>o==_user.UserId||_user.Roles.Contains("Admin")||_user.Roles.Contains("Administrator");
}
