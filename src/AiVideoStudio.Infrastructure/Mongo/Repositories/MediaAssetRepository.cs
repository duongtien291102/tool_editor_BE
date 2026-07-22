using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.Responses;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public class MediaAssetRepository : IMediaAssetRepository
{
    private readonly MongoDbContext _context;

    public MediaAssetRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<MediaAsset?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MediaAsset>.Filter.And(
            Builders<MediaAsset>.Filter.Eq(x => x.Id, id),
            Builders<MediaAsset>.Filter.Eq(x => x.DeletedAt, null)
        );

        return await _context.MediaAssets.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(MediaAsset entity, CancellationToken cancellationToken = default)
    {
        await _context.MediaAssets.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(MediaAsset entity, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MediaAsset>.Filter.Eq(x => x.Id, entity.Id);
        await _context.MediaAssets.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    public async Task<PagedResult<MediaAsset>> GetPagedByProjectIdAsync(
        string projectId,
        int page,
        int pageSize,
        string? search,
        string? sortBy,
        bool sortDescending,
        AssetType? assetType,
        MediaStatus? status,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var builder = Builders<MediaAsset>.Filter;
        var filter = builder.And(
            builder.Eq(x => x.ProjectId, projectId),
            builder.Eq(x => x.DeletedAt, null)
        );

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchRegex = new BsonRegularExpression(search.Trim(), "i");
            var searchFilter = builder.Or(
                builder.Regex(x => x.FileName, searchRegex),
                builder.Regex(x => x.OriginalFileName, searchRegex)
            );
            filter = builder.And(filter, searchFilter);
        }

        if (assetType.HasValue)
        {
            filter = builder.And(filter, builder.Eq(x => x.AssetType, assetType.Value));
        }

        if (status.HasValue)
        {
            filter = builder.And(filter, builder.Eq(x => x.Status, status.Value));
        }

        SortDefinition<MediaAsset> sortDefinition;
        var sortField = sortBy?.Trim().ToLowerInvariant();

        switch (sortField)
        {
            case "filename":
                sortDefinition = sortDescending
                    ? Builders<MediaAsset>.Sort.Descending(x => x.FileName)
                    : Builders<MediaAsset>.Sort.Ascending(x => x.FileName);
                break;
            case "originalfilename":
                sortDefinition = sortDescending
                    ? Builders<MediaAsset>.Sort.Descending(x => x.OriginalFileName)
                    : Builders<MediaAsset>.Sort.Ascending(x => x.OriginalFileName);
                break;
            case "filesize":
                sortDefinition = sortDescending
                    ? Builders<MediaAsset>.Sort.Descending(x => x.FileSize)
                    : Builders<MediaAsset>.Sort.Ascending(x => x.FileSize);
                break;
            case "assettype":
                sortDefinition = sortDescending
                    ? Builders<MediaAsset>.Sort.Descending(x => x.AssetType)
                    : Builders<MediaAsset>.Sort.Ascending(x => x.AssetType);
                break;
            case "status":
                sortDefinition = sortDescending
                    ? Builders<MediaAsset>.Sort.Descending(x => x.Status)
                    : Builders<MediaAsset>.Sort.Ascending(x => x.Status);
                break;
            case "createdat":
            default:
                sortDefinition = sortDescending
                    ? Builders<MediaAsset>.Sort.Descending(x => x.CreatedAt)
                    : Builders<MediaAsset>.Sort.Ascending(x => x.CreatedAt);
                break;
        }

        var totalCount = await _context.MediaAssets.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await _context.MediaAssets
            .Find(filter)
            .Sort(sortDefinition)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MediaAsset>(items, (int)totalCount, page, pageSize);
    }
}
