using System;
using System.Threading;
using System.Threading.Tasks;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.Responses;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AiVideoStudio.Infrastructure.Mongo.Repositories;

public class RenderJobRepository : IRenderJobRepository
{
    private readonly IMongoCollection<RenderJob> _collection;

    public RenderJobRepository(MongoDbContext context)
    {
        _collection = context.RenderJobs;
    }

    public Task<RenderJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RenderJob>.Filter.And(
            Builders<RenderJob>.Filter.Eq(x => x.Id, id),
            Builders<RenderJob>.Filter.Eq(x => x.DeletedAt, null)
        );

        return _collection.Find(filter).FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task AddAsync(RenderJob job, CancellationToken cancellationToken = default)
    {
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.InsertOneAsync(session, job, new InsertOneOptions(), cancellationToken);

        return _collection.InsertOneAsync(job, new InsertOneOptions(), cancellationToken);
    }

    public Task UpdateAsync(RenderJob job, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RenderJob>.Filter.Eq(x => x.Id, job.Id);
        
        var session = MongoTransactionContext.CurrentSession;
        if (session != null)
            return _collection.ReplaceOneAsync(session, filter, job, new ReplaceOptions(), cancellationToken);

        return _collection.ReplaceOneAsync(filter, job, new ReplaceOptions(), cancellationToken);
    }

    public async Task<PagedResult<RenderJob>> GetPagedAsync(
        string? ownerId,
        bool isAdmin,
        string? projectId,
        string? status,
        string? provider,
        string? priority,
        string? search,
        string? sortBy,
        bool sortDescending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<RenderJob>.Filter;
        var filter = filterBuilder.Eq(x => x.DeletedAt, null);

        if (!isAdmin && !string.IsNullOrEmpty(ownerId))
        {
            filter &= filterBuilder.Eq(x => x.OwnerId, ownerId);
        }

        if (!string.IsNullOrEmpty(projectId))
        {
            filter &= filterBuilder.Eq(x => x.ProjectId, projectId);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<RenderJobStatus>(status, true, out var parsedStatus))
        {
            filter &= filterBuilder.Eq(x => x.Status, parsedStatus);
        }

        if (!string.IsNullOrEmpty(provider) && Enum.TryParse<RenderProvider>(provider, true, out var parsedProvider))
        {
            filter &= filterBuilder.Eq(x => x.Provider, parsedProvider);
        }

        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<RenderPriority>(priority, true, out var parsedPriority))
        {
            filter &= filterBuilder.Eq(x => x.Priority, parsedPriority);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchRegex = new BsonRegularExpression(search.Trim(), "i");
            filter &= filterBuilder.Regex(x => x.Id, searchRegex);
        }

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var sortDefinition = GetSortDefinition(sortBy, sortDescending);

        var items = await _collection.Find(filter)
            .Sort(sortDefinition)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RenderJob>(items, (int)totalCount, page, pageSize);
    }

    public async Task<PagedResult<RenderJob>> GetByProjectIdPagedAsync(
        string projectId,
        string? status,
        string? sortBy,
        bool sortDescending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<RenderJob>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(x => x.DeletedAt, null),
            filterBuilder.Eq(x => x.ProjectId, projectId)
        );

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<RenderJobStatus>(status, true, out var parsedStatus))
        {
            filter &= filterBuilder.Eq(x => x.Status, parsedStatus);
        }

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var sortDefinition = GetSortDefinition(sortBy, sortDescending);

        var items = await _collection.Find(filter)
            .Sort(sortDefinition)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RenderJob>(items, (int)totalCount, page, pageSize);
    }

    private static SortDefinition<RenderJob> GetSortDefinition(string? sortBy, bool sortDescending)
    {
        var builder = Builders<RenderJob>.Sort;
        var field = sortBy?.Trim().ToLowerInvariant();

        return field switch
        {
            "updatedat" => sortDescending ? builder.Descending(x => x.UpdatedAt) : builder.Ascending(x => x.UpdatedAt),
            "priority" => sortDescending ? builder.Descending(x => x.Priority) : builder.Ascending(x => x.Priority),
            "status" => sortDescending ? builder.Descending(x => x.Status) : builder.Ascending(x => x.Status),
            _ => sortDescending ? builder.Descending(x => x.CreatedAt) : builder.Ascending(x => x.CreatedAt)
        };
    }
}
