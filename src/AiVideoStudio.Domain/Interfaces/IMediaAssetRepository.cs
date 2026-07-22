using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Shared.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Domain.Interfaces;

public interface IMediaAssetRepository
{
    Task<MediaAsset?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(MediaAsset entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(MediaAsset entity, CancellationToken cancellationToken = default);
    Task<PagedResult<MediaAsset>> GetPagedByProjectIdAsync(
        string projectId,
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        bool sortDescending,
        AssetType? assetType,
        MediaStatus? status,
        CancellationToken cancellationToken = default);
}
